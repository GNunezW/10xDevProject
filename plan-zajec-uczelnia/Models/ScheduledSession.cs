namespace plan_zajec_uczelnia.Models;

public class ScheduledSession
{
    public int Id { get; set; }

    public int ScheduleRunId { get; set; }
    public ScheduleRun ScheduleRun { get; set; } = null!;

    public int StudyProgramId { get; set; }
    public StudyProgram StudyProgram { get; set; } = null!;

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public int GroupIndex { get; set; }

    public int SessionIndex { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public int TimeSlotId { get; set; }
    public TimeSlot TimeSlot { get; set; } = null!;

    public string RoomNumber { get; set; } = string.Empty;
    public Room Room { get; set; } = null!;

    public int LecturerId { get; set; }
    public Lecturer Lecturer { get; set; } = null!;
}
