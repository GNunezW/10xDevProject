using plan_zajec_uczelnia.Services.Scheduling;

namespace plan_zajec_uczelnia.Tests;

/// <summary>
/// Independent collision check on a weekly-template session list.
/// Keys match the product contract (lecturer/room × day × slot), not CP-SAT internals.
/// </summary>
public static class ScheduleCollisionOracle
{
    public readonly record struct Session(
        int LecturerId,
        string RoomNumber,
        DayOfWeek Day,
        int TimeSlotId);

    public static bool HasLecturerOrRoomClash(IEnumerable<ScheduledPlacement> placements) =>
        HasLecturerOrRoomClash(placements.Select(p =>
            new Session(p.LecturerId, p.RoomNumber, p.Day, p.TimeSlotId)));

    public static bool HasLecturerOrRoomClash(IEnumerable<Session> sessions)
    {
        var list = sessions as IReadOnlyList<Session> ?? sessions.ToList();

        var lecturerClash = list
            .GroupBy(s => (s.LecturerId, s.Day, s.TimeSlotId))
            .Any(g => g.Count() > 1);

        var roomClash = list
            .GroupBy(s => (s.RoomNumber, s.Day, s.TimeSlotId))
            .Any(g => g.Count() > 1);

        return lecturerClash || roomClash;
    }
}
