using System.ComponentModel.DataAnnotations;

namespace plan_zajec_uczelnia.Models;

public class Room
{
    [Key, MaxLength(20)]
    public string RoomNumber { get; set; } = string.Empty;

    public int InstructionTypeId { get; set; }
    public InstructionType InstructionType { get; set; } = null!;
}
