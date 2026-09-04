namespace plan_zajec_uczelnia.Tests;

public class ScheduleCollisionOracleTests
{
    [Fact]
    public void Lecturer_double_booked_in_the_same_slot_is_a_clash_even_across_programs()
    {
        ScheduleCollisionOracle.Session[] sessions =
        [
            new(LecturerId: 1, RoomNumber: "A1", Day: DayOfWeek.Monday, TimeSlotId: 1),
            new(LecturerId: 1, RoomNumber: "B2", Day: DayOfWeek.Monday, TimeSlotId: 1)
        ];

        Assert.True(ScheduleCollisionOracle.HasLecturerOrRoomClash(sessions));
    }

    [Fact]
    public void Room_double_booked_in_the_same_slot_is_a_clash()
    {
        ScheduleCollisionOracle.Session[] sessions =
        [
            new(LecturerId: 1, RoomNumber: "A1", Day: DayOfWeek.Monday, TimeSlotId: 1),
            new(LecturerId: 2, RoomNumber: "A1", Day: DayOfWeek.Monday, TimeSlotId: 1)
        ];

        Assert.True(ScheduleCollisionOracle.HasLecturerOrRoomClash(sessions));
    }

    [Fact]
    public void Same_lecturer_on_the_same_day_in_different_slots_is_not_a_clash()
    {
        ScheduleCollisionOracle.Session[] sessions =
        [
            new(LecturerId: 1, RoomNumber: "A1", Day: DayOfWeek.Monday, TimeSlotId: 1),
            new(LecturerId: 1, RoomNumber: "A1", Day: DayOfWeek.Monday, TimeSlotId: 2)
        ];

        Assert.False(ScheduleCollisionOracle.HasLecturerOrRoomClash(sessions));
    }

    [Fact]
    public void Clean_list_has_no_clash()
    {
        ScheduleCollisionOracle.Session[] sessions =
        [
            new(LecturerId: 1, RoomNumber: "A1", Day: DayOfWeek.Monday, TimeSlotId: 1),
            new(LecturerId: 2, RoomNumber: "B2", Day: DayOfWeek.Tuesday, TimeSlotId: 1)
        ];

        Assert.False(ScheduleCollisionOracle.HasLecturerOrRoomClash(sessions));
    }
}
