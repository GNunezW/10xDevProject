using System.ComponentModel.DataAnnotations;

namespace plan_zajec_uczelnia.Models;

public class Room
{
    [Key, MaxLength(20)]
    public string RoomNumber { get; set; } = string.Empty;

    public RoomType Type { get; set; }

    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }

    public ICollection<RoomAvailability> Availabilities { get; set; } = [];
}
