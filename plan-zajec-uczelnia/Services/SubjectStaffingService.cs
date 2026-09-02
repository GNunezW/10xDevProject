using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Data;
using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services;

public interface ISubjectStaffingService
{
    Task<SubjectStaffingResult> EvaluateAsync(Subject subject, int studentCount, CancellationToken cancellationToken = default);
}

public class SubjectStaffingService(ApplicationDbContext db) : ISubjectStaffingService
{
    private SubjectStaffingContext? _context;

    public async Task<SubjectStaffingResult> EvaluateAsync(
        Subject subject,
        int studentCount,
        CancellationToken cancellationToken = default)
    {
        _context ??= await LoadContextAsync(cancellationToken);
        return SubjectStaffingEvaluator.Evaluate(subject, studentCount, _context);
    }

    private async Task<SubjectStaffingContext> LoadContextAsync(CancellationToken cancellationToken)
    {
        var availabilities = await db.LecturerAvailabilities
            .AsNoTracking()
            .Select(la => new { la.LecturerId, la.DayOfWeek, la.TimeSlotId })
            .ToListAsync(cancellationToken);

        var availabilityByLecturer = availabilities
            .GroupBy(a => a.LecturerId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(a => (a.DayOfWeek, a.TimeSlotId)).ToHashSet());

        var semesterPeriods = await db.SemesterPeriods
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var nonWorkingDays = await db.NonWorkingDays
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new SubjectStaffingContext
        {
            AvailabilityByLecturer = availabilityByLecturer,
            SemesterPeriods = semesterPeriods,
            NonWorkingDays = nonWorkingDays
        };
    }
}
