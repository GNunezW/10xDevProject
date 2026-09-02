using plan_zajec_uczelnia.Models;
using plan_zajec_uczelnia.Services.Scheduling;

namespace plan_zajec_uczelnia.Tests;

public class ScheduleWeekCalculatorTests
{
    // 2026-10-05 is a Monday.
    private static readonly DateOnly WeekStart = new(2026, 10, 5);
    private static readonly DateOnly WeekEnd = new(2026, 10, 11);

    [Fact]
    public void CountWeeks_InclusiveCalendarSpan_RoundsUpToFullWeeks()
    {
        Assert.Equal(1, ScheduleWeekCalculator.CountWeeks(WeekStart, WeekStart));
        Assert.Equal(1, ScheduleWeekCalculator.CountWeeks(WeekStart, WeekEnd));
        Assert.Equal(2, ScheduleWeekCalculator.CountWeeks(WeekStart, WeekEnd.AddDays(1)));
        Assert.Equal(1, ScheduleWeekCalculator.CountWeeks(WeekEnd, WeekStart));
    }

    [Fact]
    public void ResolveTeachingWeeks_FullTimeWeekWithoutHolidays_IsOneWeek()
    {
        var weeks = ScheduleWeekCalculator.ResolveTeachingWeeks(
            WeekStart,
            WeekEnd,
            StudyMode.FullTime,
            new HashSet<DateOnly>());

        Assert.Equal(1, weeks);
    }

    [Fact]
    public void ResolveTeachingWeeks_PartTimeWeekWithoutHolidays_IsOneWeek()
    {
        var weeks = ScheduleWeekCalculator.ResolveTeachingWeeks(
            WeekStart,
            WeekEnd,
            StudyMode.PartTime,
            new HashSet<DateOnly>());

        Assert.Equal(1, weeks);
    }

    [Fact]
    public void ResolveTeachingWeeks_AllAllowedDaysAreHolidays_ReturnsOne()
    {
        var weekdays = new HashSet<DateOnly>
        {
            new(2026, 10, 5),
            new(2026, 10, 6),
            new(2026, 10, 7),
            new(2026, 10, 8),
            new(2026, 10, 9)
        };

        var weeks = ScheduleWeekCalculator.ResolveTeachingWeeks(
            WeekStart,
            WeekEnd,
            StudyMode.FullTime,
            weekdays);

        Assert.Equal(1, weeks);
    }

    [Fact]
    public void ResolveWeeklySlotCount_SessionsSpreadAcrossTeachingWeeks()
    {
        var threeWeeksEnd = WeekStart.AddDays(20); // Mon 5 Oct → Sun 25 Oct
        var noHolidays = new HashSet<DateOnly>();

        Assert.Equal(
            1,
            ScheduleWeekCalculator.ResolveWeeklySlotCount(
                3, WeekStart, threeWeeksEnd, StudyMode.FullTime, noHolidays));
        Assert.Equal(
            2,
            ScheduleWeekCalculator.ResolveWeeklySlotCount(
                4, WeekStart, threeWeeksEnd, StudyMode.FullTime, noHolidays));
        Assert.Equal(
            0,
            ScheduleWeekCalculator.ResolveWeeklySlotCount(
                0, WeekStart, threeWeeksEnd, StudyMode.FullTime, noHolidays));
        Assert.Equal(
            0,
            ScheduleWeekCalculator.ResolveWeeklySlotCount(
                3, WeekEnd, WeekStart, StudyMode.FullTime, noHolidays));
    }

    [Fact]
    public void ResolveSemesterWeeks_MissingPeriod_UsesDefault()
    {
        Assert.Equal(
            ScheduleWeekCalculator.DefaultSemesterWeeks,
            ScheduleWeekCalculator.ResolveSemesterWeeks("2026/2027", []));
    }
}
