namespace plan_zajec_uczelnia.Models;

public class LecturerAvailability
{
    public int LecturerId { get; set; }
    public Lecturer Lecturer { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }

    public int TimeSlotId { get; set; }
    public TimeSlot TimeSlot { get; set; } = null!;
}
