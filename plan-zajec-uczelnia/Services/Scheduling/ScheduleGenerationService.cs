using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Data;
using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services.Scheduling;

public class ScheduleGenerationService(
    ApplicationDbContext db,
    IScheduleValidationService validation) : IScheduleGenerationService
{
    private const int SolverTimeLimitSeconds = 60;

    public async Task<ScheduleRun> GenerateAsync(CancellationToken cancellationToken = default)
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
                await FailRunAsync(run, string.Join(" ", check.Issues), cancellationToken);
                return run;
            }

            var tasks = await BuildPlacementTasksAsync(cancellationToken);
            if (tasks.Count == 0)
            {
                await FailRunAsync(run, "Brak sesji do zaplanowania.", cancellationToken);
                return run;
            }

            var slots = await db.TimeSlots.AsNoTracking().OrderBy(s => s.StartTime).ToListAsync(cancellationToken);
            var slotIndexByTimeSlotId = slots
                .Select((s, idx) => (s.Id, idx))
                .ToDictionary(x => x.Id, x => x.idx);

            var solver = new ScheduleCpSatSolver();
            var result = solver.Solve(tasks, slotIndexByTimeSlotId, SolverTimeLimitSeconds);

            if (!result.Success)
            {
                await FailRunAsync(run, result.ErrorMessage ?? "Generowanie nie powiodło się.", cancellationToken);
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
            await FailRunAsync(run, ex.Message, cancellationToken);
            return run;
        }
    }

    private async Task FailRunAsync(ScheduleRun run, string message, CancellationToken ct)
    {
        run.Status = ScheduleRunStatus.Failed;
        run.ErrorMessage = message.Length > 2000 ? message[..2000] : message;
        run.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

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

        var subjects = await db.Subjects
            .AsNoTracking()
            .Include(s => s.InstructionType)
            .Include(s => s.SubjectLecturers)
            .ToListAsync(ct);

        var tasks = new List<PlacementTask>();
        var taskIndex = 0;

        foreach (var subject in subjects)
        {
            if (!programs.TryGetValue(subject.StudyProgramId, out var program))
                continue;

            var primary = subject.SubjectLecturers.FirstOrDefault(sl => sl.IsPrimary);
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

            var allowedDays = program.StudyMode == StudyMode.FullTime
                ? new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }
                : new[] { DayOfWeek.Saturday, DayOfWeek.Sunday };

            var allowed = new List<ScheduleAssignment>();
            foreach (var day in allowedDays)
            {
                foreach (var slot in slots)
                {
                    if (!availabilitySet.Contains((primary.LecturerId, day, slot.Id)))
                        continue;

                    foreach (var room in roomNumbers)
                        allowed.Add(new ScheduleAssignment(day, slot.Id, room));
                }
            }

            for (var group = 1; group <= groupCount; group++)
            {
                for (var session = 1; session <= subject.NumberOfSessions; session++)
                {
                    tasks.Add(new PlacementTask
                    {
                        TaskIndex = taskIndex++,
                        StudyProgramId = subject.StudyProgramId,
                        SubjectId = subject.Id,
                        GroupIndex = group,
                        SessionIndex = session,
                        InstructionTypeId = subject.InstructionTypeId,
                        LecturerId = primary.LecturerId,
                        AllowedAssignments = allowed
                    });
                }
            }
        }

        return tasks;
    }
}
