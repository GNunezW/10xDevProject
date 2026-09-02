using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services.Scheduling;

public static class ScheduleWeekCalculator
{
    public const int DefaultSemesterWeeks = 15;

    public static int CountWeeks(DateOnly start, DateOnly end)
    {
        if (end < start)
            return 1;

        var days = end.DayNumber - start.DayNumber + 1;
        return Math.Max(1, (int)Math.Ceiling(days / 7.0));
    }

    public static int ResolveSemesterWeeks(
        string academicYear,
        IReadOnlyList<SemesterPeriod> periods)
    {
        var matching = periods
            .Where(p => p.AcademicYear == academicYear)
            .ToList();

        if (matching.Count == 0)
            return DefaultSemesterWeeks;

        return matching.Max(p => CountWeeks(p.StartDate, p.EndDate));
    }

    public static int CountTeachingDays(
        DateOnly start,
        DateOnly end,
        IReadOnlyList<DayOfWeek> allowedDays,
        IReadOnlySet<DateOnly> nonWorkingDays)
    {
        var count = 0;
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (!allowedDays.Contains(date.DayOfWeek))
                continue;
            if (nonWorkingDays.Contains(date))
                continue;
            count++;
        }

        return count;
    }

    /// <summary>
    /// Tygodnie dydaktyczne = dni robocze (wg trybu studiów) minus święta, przeliczone na pełne tygodnie.
    /// </summary>
    public static int ResolveTeachingWeeks(
        DateOnly start,
        DateOnly end,
        StudyMode studyMode,
        IReadOnlySet<DateOnly> nonWorkingDays)
    {
        var allowedDays = ScheduleGridData.GetDaysForMode(studyMode);
        var teachingDays = CountTeachingDays(start, end, allowedDays, nonWorkingDays);
        if (teachingDays <= 0)
            return 1;

        return Math.Max(1, (int)Math.Ceiling(teachingDays / (double)allowedDays.Count));
    }

    /// <summary>
    /// Ile slotów tygodniowo w szablonie, by zmieścić wszystkie spotkania przed końcem semestru (z uwzgl. świąt).
    /// </summary>
    public static int ResolveWeeklySlotCount(
        int numberOfSessions,
        DateOnly semesterStart,
        DateOnly semesterEnd,
        StudyMode studyMode,
        IReadOnlySet<DateOnly> nonWorkingDays)
    {
        if (numberOfSessions <= 0 || semesterEnd < semesterStart)
            return 0;

        var teachingWeeks = ResolveTeachingWeeks(semesterStart, semesterEnd, studyMode, nonWorkingDays);
        return Math.Max(1, (int)Math.Ceiling(numberOfSessions / (double)teachingWeeks));
    }

    /// <summary>Maks. liczba spotkań, które zmieszczą się w semestrze przy danej częstotliwości tygodniowej.</summary>
    public static int MaxMeetingsInSemester(
        DateOnly semesterStart,
        DateOnly semesterEnd,
        StudyMode studyMode,
        int weeklySlotCount,
        IReadOnlySet<DateOnly> nonWorkingDays)
    {
        if (weeklySlotCount <= 0 || semesterEnd < semesterStart)
            return 0;

        var allowedDays = ScheduleGridData.GetDaysForMode(studyMode);
        var teachingDays = CountTeachingDays(semesterStart, semesterEnd, allowedDays, nonWorkingDays);
        var daysPerWeek = allowedDays.Count;
        var teachingWeeks = Math.Max(1, (int)Math.Ceiling(teachingDays / (double)daysPerWeek));
        return weeklySlotCount * teachingWeeks;
    }
}
