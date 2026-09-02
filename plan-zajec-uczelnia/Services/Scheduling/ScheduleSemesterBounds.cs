using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services.Scheduling;

public static class ScheduleSemesterBounds
{
    public static (DateOnly? Start, DateOnly? End) Resolve(
        string academicYear,
        IReadOnlyList<SemesterPeriod> periods)
    {
        var matching = periods
            .Where(p => p.AcademicYear == academicYear)
            .ToList();

        if (matching.Count == 0)
            return (null, null);

        return (matching.Min(p => p.StartDate), matching.Max(p => p.EndDate));
    }

    public static HashSet<DateOnly> ResolveNonWorkingDays(
        string academicYear,
        IReadOnlyList<NonWorkingDay> days) =>
        days
            .Where(d => d.AcademicYear is null || d.AcademicYear == academicYear)
            .Select(d => d.Date)
            .ToHashSet();
}
