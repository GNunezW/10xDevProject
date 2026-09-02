namespace plan_zajec_uczelnia.Services.Scheduling;

public static class ScheduleCalendarHelper
{
    /// <summary>
    /// Mapuje N-te spotkanie semestralne na datę kalendarzową (pomija dni wolne).
    /// </summary>
    public static DateOnly? ResolveMeetingDate(
        DateOnly semesterStart,
        DateOnly semesterEnd,
        int totalMeetings,
        int weeklySlotCount,
        int meetingNumber,
        DayOfWeek dayOfWeek,
        IReadOnlySet<DateOnly> nonWorkingDays)
    {
        if (totalMeetings <= 0 || weeklySlotCount <= 0 || meetingNumber <= 0 || meetingNumber > totalMeetings)
            return null;

        var weekIndex = (meetingNumber - 1) / weeklySlotCount + 1;
        return FindNthValidWeekdayOccurrence(semesterStart, semesterEnd, dayOfWeek, weekIndex, nonWorkingDays);
    }

    public static DateOnly? ResolveLastMeetingDate(
        DateOnly semesterStart,
        DateOnly semesterEnd,
        int totalMeetings,
        int weeklySlotCount,
        DayOfWeek dayOfWeek,
        IReadOnlySet<DateOnly> nonWorkingDays) =>
        ResolveMeetingDate(
            semesterStart,
            semesterEnd,
            totalMeetings,
            weeklySlotCount,
            totalMeetings,
            dayOfWeek,
            nonWorkingDays);

    private static DateOnly? FindNthValidWeekdayOccurrence(
        DateOnly start,
        DateOnly end,
        DayOfWeek dayOfWeek,
        int occurrenceIndex,
        IReadOnlySet<DateOnly> nonWorkingDays)
    {
        if (occurrenceIndex <= 0)
            return null;

        var count = 0;
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (date.DayOfWeek != dayOfWeek || nonWorkingDays.Contains(date))
                continue;

            count++;
            if (count == occurrenceIndex)
                return date;
        }

        return null;
    }
}
