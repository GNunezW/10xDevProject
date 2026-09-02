using System.ComponentModel.DataAnnotations;

namespace plan_zajec_uczelnia.Models;

public class StudyProgram
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public StudyMode StudyMode { get; set; }

    public StudyDegree StudyDegree { get; set; } = StudyDegree.FirstDegreeEngineer;

    [Required, MaxLength(20)]
    public string AcademicYear { get; set; } = string.Empty;

    public ICollection<Subject> Subjects { get; set; } = [];
}
