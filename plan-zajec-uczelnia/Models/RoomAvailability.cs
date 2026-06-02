namespace plan_zajec_uczelnia.Models;

public class RoomAvailability
{
    public string RoomNumber { get; set; } = string.Empty;
    public Room Room { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }

    public int TimeSlotId { get; set; }
    public TimeSlot TimeSlot { get; set; } = null!;
}
