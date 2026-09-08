using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Data;
using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services.Scheduling;

public class ScheduleGenerationService(
    ApplicationDbContext db,
    IScheduleValidationService validation,
    ILogger<ScheduleGenerationService> logger) : IScheduleGenerationService
{
    private const int SolverTimeLimitSeconds = 60;
    private static readonly SemaphoreSlim GenerationLock = new(1, 1);

    public async Task<ScheduleRun> GenerateAsync(CancellationToken cancellationToken = default)
    {
        if (!await GenerationLock.WaitAsync(0, cancellationToken))
        {
            return await CreateImmediateFailedRunAsync(
                "Generowanie planu jest już w toku — poczekaj na zakończenie bieżącego uruchomienia.",
                cancellationToken);
        }

        try
        {
            return await GenerateCoreAsync(cancellationToken);
        }
        finally
        {
            GenerationLock.Release();
        }
    }

    private async Task<ScheduleRun> GenerateCoreAsync(CancellationToken cancellationToken)
    {
        var run = new ScheduleRun
        {
            StartedAt = DateTime.UtcNow,
            Status = ScheduleRunStatus.Running,
            SolverTimeLimitSeconds = SolverTimeLimitSeconds
        };
        db.ScheduleRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var check = await validation.ValidateAsync(cancellationToken);
            if (!check.CanRun)
            {
                await FailRunAsync(run, string.Join(Environment.NewLine, check.Issues), cancellationToken);
                return run;
            }

            var tasks = await BuildPlacementTasksAsync(cancellationToken);
            if (tasks.Count == 0)
            {
                await FailRunAsync(run, "Brak sesji do zaplanowania.", cancellationToken);
                return run;
            }

            var lecturerNamesById = await db.Lecturers.AsNoTracking()
                .ToDictionaryAsync(
                    l => l.Id,
                    l => $"{l.FirstName} {l.LastName}",
                    cancellationToken);

            var slots = await db.TimeSlots.AsNoTracking().OrderBy(s => s.StartTime).ToListAsync(cancellationToken);
            var slotIndexByTimeSlotId = slots
                .Select((s, idx) => (s.Id, idx))
                .ToDictionary(x => x.Id, x => x.idx);
            var slotStartById = slots.ToDictionary(s => s.Id, s => s.StartTime);

            var solver = new ScheduleCpSatSolver();
            var result = await Task.Run(
                () => solver.Solve(tasks, slotIndexByTimeSlotId, SolverTimeLimitSeconds),
                cancellationToken);

            if (!result.Success)
            {
                string message;
                try
                {
                    message = tasks.Count > 0
                        ? ScheduleInfeasibilityDiagnostics.BuildMessage(
                            tasks, slotStartById, lecturerNamesById, result.SolverStatus)
                        : "Nie znaleziono planu spełniającego ograniczenia.";
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Infeasibility diagnostics failed for schedule run {RunId}", run.Id);
                    message = $"Nie znaleziono planu spełniającego ograniczenia. (Błąd diagnostyki: {ex.Message})";
                }

                await FailRunAsync(run, message, cancellationToken);
                return run;
            }

            foreach (var p in result.Placements)
            {
                db.ScheduledSessions.Add(new ScheduledSession
                {
                    ScheduleRunId = run.Id,
                    StudyProgramId = p.StudyProgramId,
                    SubjectId = p.SubjectId,
                    GroupIndex = p.GroupIndex,
                    SessionIndex = p.SessionIndex,
                    DayOfWeek = p.Day,
                    TimeSlotId = p.TimeSlotId,
                    RoomNumber = p.RoomNumber,
                    LecturerId = p.LecturerId
                });
            }

            run.Status = ScheduleRunStatus.Succeeded;
            run.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return run;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Schedule generation failed for run {RunId}", run.Id);
            await FailRunAsync(run, SchedulePersistenceHelper.FormatException(ex), cancellationToken);
            return run;
        }
    }

    private async Task<ScheduleRun> CreateImmediateFailedRunAsync(string message, CancellationToken ct)
    {
        var run = new ScheduleRun
        {
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Status = ScheduleRunStatus.Failed,
            SolverTimeLimitSeconds = SolverTimeLimitSeconds,
            ErrorMessage = message
        };
        db.ScheduleRuns.Add(run);
        await db.SaveChangesAsync(ct);
        return run;
    }

    private Task FailRunAsync(ScheduleRun run, string message, CancellationToken ct) =>
        SchedulePersistenceHelper.SaveFailedRunAsync(db, run, message, ct, logger);

    private async Task<List<PlacementTask>> BuildPlacementTasksAsync(CancellationToken ct)
    {
        var programs = await db.StudyPrograms.AsNoTracking().ToDictionaryAsync(p => p.Id, ct);
        var slots = await db.TimeSlots.AsNoTracking().OrderBy(s => s.StartTime).ToListAsync(ct);
        var rooms = await db.Rooms.AsNoTracking().ToListAsync(ct);
        var roomsByType = rooms.GroupBy(r => r.InstructionTypeId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.RoomNumber).ToList());

        var availability = await db.LecturerAvailabilities.AsNoTracking().ToListAsync(ct);
        var availabilitySet = availability
            .Select(a => (a.LecturerId, a.DayOfWeek, a.TimeSlotId))
            .ToHashSet();

        var enrollments = await db.StudyProgramEnrollments.AsNoTracking().ToListAsync(ct);
        var enrollmentLookup = enrollments.ToDictionary(e => (e.StudyProgramId, e.Semester));

        var lecturers = await db.Lecturers.AsNoTracking().ToDictionaryAsync(l => l.Id, ct);

        var semesterPeriods = await db.SemesterPeriods.AsNoTracking().ToListAsync(ct);
        var nonWorkingDays = await db.NonWorkingDays.AsNoTracking().ToListAsync(ct);

        var subjects = await db.Subjects
            .AsNoTracking()
            .Include(s => s.StudyProgram)
            .Include(s => s.InstructionType)
            .Include(s => s.SubjectLecturers)
            .ToListAsync(ct);

        var tasks = new List<PlacementTask>();
        var taskIndex = 0;

        foreach (var subject in subjects)
        {
            if (!programs.TryGetValue(subject.StudyProgramId, out var program))
                continue;

            var subjectLecturers = subject.SubjectLecturers
                .OrderByDescending(sl => sl.IsPrimary)
                .ThenBy(sl => sl.LecturerId)
                .ToList();
            if (subjectLecturers.Count == 0)
                continue;

            var primary = subjectLecturers.FirstOrDefault(sl => sl.IsPrimary);
            if (primary is null)
                continue;

            if (!enrollmentLookup.TryGetValue((subject.StudyProgramId, subject.Semester), out var enrollment))
                continue;

            var groupCount = InstructionType.EstimateGroupCount(
                enrollment.StudentCount, subject.InstructionType);
            if (groupCount <= 0)
                continue;

            if (!roomsByType.TryGetValue(subject.InstructionTypeId, out var roomNumbers)
                || roomNumbers.Count == 0)
                continue;

            var allowedDays = ScheduleGridData.GetDaysForMode(program.StudyMode);

            var allowed = new List<ScheduleAssignment>();
            foreach (var subjectLecturer in subjectLecturers)
            {
                foreach (var day in allowedDays)
                {
                    foreach (var slot in slots)
                    {
                        if (!availabilitySet.Contains((subjectLecturer.LecturerId, day, slot.Id)))
                            continue;

                        foreach (var room in roomNumbers)
                        {
                            allowed.Add(new ScheduleAssignment(
                                subjectLecturer.LecturerId,
                                day,
                                slot.Id,
                                room,
                                subjectLecturer.IsPrimary));
                        }
                    }
                }
            }

            if (allowed.Count == 0)
                continue;

            lecturers.TryGetValue(primary.LecturerId, out var primaryLecturer);
            var primaryLecturerName = primaryLecturer is not null
                ? $"{primaryLecturer.FirstName} {primaryLecturer.LastName}"
                : $"ID {primary.LecturerId}";

            var (semesterStart, semesterEnd) = ScheduleSemesterBounds.Resolve(
                program.AcademicYear, semesterPeriods);
            if (semesterStart is null || semesterEnd is null)
                continue;

            var holidays = ScheduleSemesterBounds.ResolveNonWorkingDays(
                program.AcademicYear, nonWorkingDays);

            var weeklySlotCount = ScheduleWeekCalculator.ResolveWeeklySlotCount(
                subject.NumberOfSessions,
                semesterStart.Value,
                semesterEnd.Value,
                program.StudyMode,
                holidays);
            if (weeklySlotCount <= 0)
                continue;

            for (var group = 1; group <= groupCount; group++)
            {
                for (var weeklySlot = 1; weeklySlot <= weeklySlotCount; weeklySlot++)
                {
                    tasks.Add(new PlacementTask
                    {
                        TaskIndex = taskIndex++,
                        StudyProgramId = subject.StudyProgramId,
                        Semester = subject.Semester,
                        SubjectId = subject.Id,
                        GroupIndex = group,
                        SessionIndex = weeklySlot,
                        InstructionTypeId = subject.InstructionTypeId,
                        PrimaryLecturerId = primary.LecturerId,
                        SubjectName = subject.Name,
                        StudyProgramName = program.Name,
                        PrimaryLecturerName = primaryLecturerName,
                        AllowedAssignments = allowed
                    });
                }
            }
        }

        return tasks;
    }
}
