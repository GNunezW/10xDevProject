using System.ComponentModel.DataAnnotations;

namespace plan_zajec_uczelnia.Models;

public class SemesterPeriod
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string AcademicYear { get; set; } = string.Empty;

    /// <summary>1 = zimowy, 2 = letni</summary>
    [Range(1, 2)]
    public int SemesterOrdinal { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
