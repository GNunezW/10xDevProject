namespace plan_zajec_uczelnia.Models;

public class SubjectLecturer
{
    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public int LecturerId { get; set; }
    public Lecturer Lecturer { get; set; } = null!;
}
