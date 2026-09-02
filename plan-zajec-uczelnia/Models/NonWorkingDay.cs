using System.ComponentModel.DataAnnotations;

namespace plan_zajec_uczelnia.Models;

/// <summary>Dzień wolny od zajęć (święto państwowe, przerwa). Pusty AcademicYear = każdy rok.</summary>
public class NonWorkingDay
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string? AcademicYear { get; set; }

    public DateOnly Date { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
