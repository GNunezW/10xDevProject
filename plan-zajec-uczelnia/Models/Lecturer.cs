using System.ComponentModel.DataAnnotations;

namespace plan_zajec_uczelnia.Models;

public class Lecturer
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    public ICollection<SubjectLecturer> SubjectLecturers { get; set; } = [];
}
