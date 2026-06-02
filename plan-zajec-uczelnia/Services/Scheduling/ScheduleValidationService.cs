using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Data;
using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services.Scheduling;

public class ScheduleValidationService(ApplicationDbContext db) : IScheduleValidationService
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

        foreach (var subject in subjects)
        {
            var label = $"Przedmiot „{subject.Name}”";

            if (subject.SubjectLecturers.Count == 0)
            {
                issues.Add($"{label}: brak przypisanych prowadzących.");
                continue;
            }

            var primaryCount = subject.SubjectLecturers.Count(sl => sl.IsPrimary);
            if (primaryCount != 1)
                issues.Add($"{label}: wymagany dokładnie jeden główny prowadzący (obecnie: {primaryCount}).");

            var primary = subject.SubjectLecturers.FirstOrDefault(sl => sl.IsPrimary);
            if (primary is not null && !lecturersWithAvailability.Contains(primary.LecturerId))
                issues.Add($"{label}: główny prowadzący nie ma żadnej dostępności w siatce.");

            if (!enrollmentLookup.TryGetValue((subject.StudyProgramId, subject.Semester), out var enrollment)
                || enrollment.StudentCount <= 0)
            {
                issues.Add($"{label}: brak liczebności studentów dla kierunku i semestru {subject.Semester}.");
            }
            else
            {
                var groups = InstructionType.EstimateGroupCount(
                    enrollment.StudentCount, subject.InstructionType);
                if (groups <= 0)
                    issues.Add($"{label}: szacowana liczba grup wynosi 0 — sprawdź liczebność lub typ zajęć.");
            }

            if (!roomsByTypeSet.Contains(subject.InstructionTypeId))
                issues.Add($"{label}: brak sal dla typu zajęć „{subject.InstructionType.Name}”.");
        }

        return new ScheduleValidationResult(issues.Count == 0, issues);
    }
}
