namespace plan_zajec_uczelnia.Services.Scheduling;

public sealed class PlacementTask
{
    public required int TaskIndex { get; init; }
    public required int StudyProgramId { get; init; }
    public required int Semester { get; init; }
    public required int SubjectId { get; init; }
    public required int GroupIndex { get; init; }
    public required int SessionIndex { get; init; }
    public required int InstructionTypeId { get; init; }
    public required int PrimaryLecturerId { get; init; }
    public required string SubjectName { get; init; }
    public required string StudyProgramName { get; init; }
    public required string PrimaryLecturerName { get; init; }
    public required IReadOnlyList<ScheduleAssignment> AllowedAssignments { get; init; }
}

public readonly record struct ScheduleAssignment(
    int LecturerId,
    DayOfWeek Day,
    int TimeSlotId,
    string RoomNumber,
    bool IsPrimary = false);
