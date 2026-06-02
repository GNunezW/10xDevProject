namespace plan_zajec_uczelnia.Services.Scheduling;

public sealed class PlacementTask
{
    public required int TaskIndex { get; init; }
    public required int StudyProgramId { get; init; }
    public required int SubjectId { get; init; }
    public required int GroupIndex { get; init; }
    public required int SessionIndex { get; init; }
    public required int InstructionTypeId { get; init; }
    public required int LecturerId { get; init; }
    public required IReadOnlyList<ScheduleAssignment> AllowedAssignments { get; init; }
}

public readonly record struct ScheduleAssignment(
    DayOfWeek Day,
    int TimeSlotId,
    string RoomNumber);
