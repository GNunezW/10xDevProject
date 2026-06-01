namespace plan_zajec_uczelnia.Models;

public class TimeSlot
{
    public int Id { get; set; }

    /// <summary>Godzina startu bloku. Constraint: >= 08:00 i StartTime + 90 min <= 20:00 (walidacja w serwisie).</summary>
    public TimeOnly StartTime { get; set; }
}
