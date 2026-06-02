using System.ComponentModel.DataAnnotations;

namespace plan_zajec_uczelnia.Models;

public class StudyProgramEnrollment
{
    public int StudyProgramId { get; set; }
    public StudyProgram StudyProgram { get; set; } = null!;

    [Range(1, 7)]
    public int Semester { get; set; }

    [Range(0, int.MaxValue)]
    public int StudentCount { get; set; }
}
