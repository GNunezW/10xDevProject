using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Data;
using plan_zajec_uczelnia.Models;
using plan_zajec_uczelnia.Services;

namespace plan_zajec_uczelnia.Services.Scheduling;

public class ScheduleValidationService(
    ApplicationDbContext db,
    ISubjectStaffingService staffingService) : IScheduleValidationService
{
    public async Task<ScheduleValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var issues = new List<string>();

        if (!await db.StudyPrograms.AnyAsync(cancellationToken))
            issues.Add("Brak kierunków studiów — dodaj co najmniej jeden kierunek.");

        if (!await db.TimeSlots.AnyAsync(cancellationToken))
            issues.Add("Brak siatki godzin — zdefiniuj co najmniej jeden slot na stronie Siatka.");

        var subjects = await db.Subjects
            .AsNoTracking()
            .Include(s => s.InstructionType)
            .Include(s => s.StudyProgram)
            .Include(s => s.SubjectLecturers)
            .ToListAsync(cancellationToken);

        if (subjects.Count == 0)
            issues.Add("Brak przedmiotów — dodaj co najmniej jeden przedmiot.");
        else if (!subjects.Any(s => s.NumberOfSessions >= 1))
            issues.Add("Brak przedmiotów z liczbą zajęć w semestrze ≥ 1.");

        var enrollments = await db.StudyProgramEnrollments
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var enrollmentLookup = enrollments.ToDictionary(e => (e.StudyProgramId, e.Semester));

        var periods = await db.SemesterPeriods.AsNoTracking().ToListAsync(cancellationToken);
        var nonWorkingDayRows = await db.NonWorkingDays.AsNoTracking().ToListAsync(cancellationToken);

        var roomsByType = await db.Rooms
            .AsNoTracking()
            .GroupBy(r => r.InstructionTypeId)
            .Select(g => g.Key)
            .ToListAsync(cancellationToken);

        var roomsByTypeSet = roomsByType.ToHashSet();

        var availabilityByLecturer = await db.LecturerAvailabilities
            .AsNoTracking()
            .Select(la => la.LecturerId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var lecturersWithAvailability = availabilityByLecturer.ToHashSet();

        var lecturerNames = await db.Lecturers
            .AsNoTracking()
            .ToDictionaryAsync(l => l.Id, l => $"{l.FirstName} {l.LastName}", cancellationToken);

        foreach (var subject in subjects)
        {
            var label = $"Przedmiot „{subject.Name}”";
            var program = subject.StudyProgram;
            var maxSemester = program is not null
                ? StudyDegreeRules.MaxSemesters(program.StudyDegree)
                : 7;

            if (subject.Semester > maxSemester)
            {
                issues.Add(
                    $"{label}: semestr {subject.Semester} przekracza limit ({maxSemester}) dla studiów {StudyDegreeRules.ShortLabel(program!.StudyDegree)}.");
                continue;
            }

            if (subject.SubjectLecturers.Count == 0)
            {
                issues.Add($"{label}: brak przypisanych prowadzących.");
                continue;
            }

            var primaryCount = subject.SubjectLecturers.Count(sl => sl.IsPrimary);
            if (primaryCount != 1)
                issues.Add($"{label}: wymagany dokładnie jeden główny prowadzący (obecnie: {primaryCount}).");

            var primary = subject.SubjectLecturers.FirstOrDefault(sl => sl.IsPrimary);
            var lecturersWithAvailabilityOnSubject = subject.SubjectLecturers
                .Where(sl => lecturersWithAvailability.Contains(sl.LecturerId))
                .ToList();

            if (lecturersWithAvailabilityOnSubject.Count == 0)
                issues.Add($"{label}: żaden przypisany prowadzący nie ma dostępności w siatce.");

            if (!enrollmentLookup.TryGetValue((subject.StudyProgramId, subject.Semester), out var enrollment)
                || enrollment.StudentCount <= 0)
            {
                issues.Add($"{label}: brak liczebności studentów dla kierunku i semestru {subject.Semester}.");
            }
            else if (subject.InstructionType is null)
            {
                issues.Add($"{label}: brak typu zajęć.");
            }
            else
            {
                var groups = InstructionType.EstimateGroupCount(
                    enrollment.StudentCount, subject.InstructionType);
                if (groups <= 0)
                    issues.Add($"{label}: szacowana liczba grup wynosi 0 — sprawdź liczebność lub typ zajęć.");
                else
                {
                    var staffing = await staffingService.EvaluateAsync(
                        subject, enrollment.StudentCount, cancellationToken);
                    if (!staffing.IsSufficient)
                        issues.Add($"{label}: {staffing.Message}.");

                    if (program is not null)
                    {
                        var (semesterStart, semesterEnd) = ScheduleSemesterBounds.Resolve(
                            program.AcademicYear, periods);
                    if (semesterStart is not null && semesterEnd is not null)
                    {
                        var holidays = ScheduleSemesterBounds.ResolveNonWorkingDays(
                            program.AcademicYear, nonWorkingDayRows);
                        var weeklySlots = ScheduleWeekCalculator.ResolveWeeklySlotCount(
                            subject.NumberOfSessions,
                            semesterStart.Value,
                            semesterEnd.Value,
                            program.StudyMode,
                            holidays);
                        var capacity = ScheduleWeekCalculator.MaxMeetingsInSemester(
                            semesterStart.Value,
                            semesterEnd.Value,
                            program.StudyMode,
                            weeklySlots,
                            holidays);
                        if (subject.NumberOfSessions > capacity)
                        {
                            issues.Add(
                                $"{label}: {subject.NumberOfSessions} spotkań nie zmieści się przed {semesterEnd:dd.MM.yyyy} " +
                                $"(przy {weeklySlots}/tydz., max {capacity} z uwzgl. dni wolnych).");
                        }
                    }
                    }
                }
            }

            if (!roomsByTypeSet.Contains(subject.InstructionTypeId))
            {
                var typeName = subject.InstructionType?.Name ?? "nieznany";
                issues.Add($"{label}: brak sal dla typu zajęć „{typeName}”.");
            }
        }

        return new ScheduleValidationResult(issues.Count == 0, issues);
    }
}
