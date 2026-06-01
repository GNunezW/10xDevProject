using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Data;
using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services;

public class TimeSlotService(ApplicationDbContext db) : ITimeSlotService
{
    private static readonly TimeOnly MinStart = new(8, 0);
    private static readonly TimeOnly MaxStart = new(18, 30); // 18:30 + 90 min = 20:00

    public Task<List<TimeSlot>> GetAllAsync() =>
        db.TimeSlots.OrderBy(t => t.StartTime).ToListAsync();

    public async Task<TimeSlot> CreateAsync(TimeSlot slot)
    {
        if (slot.StartTime < MinStart || slot.StartTime > MaxStart)
            throw new InvalidOperationException(
                "Godzina startu musi być między 08:00 a 18:30 (blok kończy się o 20:00).");

        var duplicate = await db.TimeSlots.AnyAsync(t => t.StartTime == slot.StartTime);
        if (duplicate)
            throw new InvalidOperationException(
                $"Slot {slot.StartTime:HH\\:mm} już istnieje.");

        db.TimeSlots.Add(slot);
        await db.SaveChangesAsync();
        return slot;
    }

    public async Task DeleteAsync(int id)
    {
        var slot = await db.TimeSlots.FindAsync(id);
        if (slot is not null)
        {
            db.TimeSlots.Remove(slot);
            await db.SaveChangesAsync();
        }
    }
}
