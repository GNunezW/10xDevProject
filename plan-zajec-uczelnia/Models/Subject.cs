using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace plan_zajec_uczelnia.Models;

public class Subject
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public int StudyProgramId { get; set; }
    public StudyProgram StudyProgram { get; set; } = null!;

    [Range(1, 7)]
    public int Semester { get; set; }

    /// <summary>Liczba bloków 90-minutowych do zaplanowania w semestrze.</summary>
    [Range(1, int.MaxValue)]
    public int NumberOfSessions { get; set; }

    public ICollection<SubjectLecturer> SubjectLecturers { get; set; } = [];
}
