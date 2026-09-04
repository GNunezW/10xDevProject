using plan_zajec_uczelnia.Models;
using plan_zajec_uczelnia.Services.Scheduling;

namespace plan_zajec_uczelnia.Tests;

public class ScheduleGridDataTests
{
    [Fact]
    public void FullTime_days_are_weekdays_only()
    {
        var days = ScheduleGridData.GetDaysForMode(StudyMode.FullTime);

        Assert.Equal(
            [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday],
            days);
        Assert.DoesNotContain(DayOfWeek.Saturday, days);
        Assert.DoesNotContain(DayOfWeek.Sunday, days);
    }

    [Fact]
    public void PartTime_days_are_weekend_only()
    {
        var days = ScheduleGridData.GetDaysForMode(StudyMode.PartTime);

        Assert.Equal([DayOfWeek.Saturday, DayOfWeek.Sunday], days);
        Assert.DoesNotContain(DayOfWeek.Monday, days);
    }
}
