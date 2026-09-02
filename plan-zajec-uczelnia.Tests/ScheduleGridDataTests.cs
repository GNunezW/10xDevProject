using plan_zajec_uczelnia.Models;
using plan_zajec_uczelnia.Services.Scheduling;

namespace plan_zajec_uczelnia.Tests;

public class ScheduleGridDataTests
{
    [Fact]
    public void GetDaysForMode_FullTime_ReturnsWeekdays()
    {
        var days = ScheduleGridData.GetDaysForMode(StudyMode.FullTime);

        Assert.Equal(
            [
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday
            ],
            days);
    }

    [Fact]
    public void GetDaysForMode_PartTime_ReturnsWeekend()
    {
        var days = ScheduleGridData.GetDaysForMode(StudyMode.PartTime);

        Assert.Equal([DayOfWeek.Saturday, DayOfWeek.Sunday], days);
    }
}
