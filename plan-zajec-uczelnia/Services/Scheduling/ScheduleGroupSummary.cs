namespace plan_zajec_uczelnia.Services.Scheduling;

public sealed class ScheduleGroupSummary
{
    public required string SubjectName { get; init; }
    public int GroupIndex { get; init; }
    public int MeetingsInSemester { get; init; }
    public int WeeklySlots { get; init; }
    public DateOnly? LastSessionDate { get; init; }
}
