using System.ComponentModel.DataAnnotations;

namespace plan_zajec_uczelnia.Models;

/// <summary>Typ zajęć i sal — nazwa + limit osób w grupie (źródło prawdy dla pojemności sal tego typu).</summary>
public class InstructionType
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int MaxStudentsPerGroup { get; set; } = 30;

    public static int EstimateGroupCount(int studentCount, InstructionType type)
    {
        if (studentCount <= 0) return 0;
        return (int)Math.Ceiling(studentCount / (double)type.MaxStudentsPerGroup);
    }
}
