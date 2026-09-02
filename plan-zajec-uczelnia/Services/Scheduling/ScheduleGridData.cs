using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services.Scheduling;

public readonly record struct ScheduleGridCellKey(DayOfWeek Day, int TimeSlotId);

public sealed class ScheduleGridData
{
    public required IReadOnlyList<TimeSlot> TimeSlots { get; init; }
    public required IReadOnlyList<DayOfWeek> Days { get; init; }
    public required IReadOnlyDictionary<ScheduleGridCellKey, IReadOnlyList<ScheduledSession>> Cells { get; init; }

    public static IReadOnlyList<DayOfWeek> GetDaysForMode(StudyMode mode) =>
        mode == StudyMode.FullTime
            ?
            [
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday
            ]
            : [DayOfWeek.Saturday, DayOfWeek.Sunday];
}
