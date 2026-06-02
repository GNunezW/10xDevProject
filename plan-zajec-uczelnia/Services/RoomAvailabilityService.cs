using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Data;
using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services;

public class RoomAvailabilityService(ApplicationDbContext db) : IRoomAvailabilityService
{
    public async Task<HashSet<(DayOfWeek Day, int TimeSlotId)>> GetByRoomAsync(string roomNumber)
    {
        var rows = await db.RoomAvailabilities
            .AsNoTracking()
            .Where(ra => ra.RoomNumber == roomNumber)
            .Select(ra => new { ra.DayOfWeek, ra.TimeSlotId })
            .ToListAsync();

        return rows.Select(r => (r.DayOfWeek, r.TimeSlotId)).ToHashSet();
    }

    public async Task SaveAsync(string roomNumber, IEnumerable<(DayOfWeek Day, int TimeSlotId)> slots)
    {
        var existing = db.RoomAvailabilities.Where(ra => ra.RoomNumber == roomNumber);
        db.RoomAvailabilities.RemoveRange(existing);

        var newEntries = slots.Select(s => new RoomAvailability
        {
            RoomNumber = roomNumber,
            DayOfWeek = s.Day,
            TimeSlotId = s.TimeSlotId
        });

        await db.RoomAvailabilities.AddRangeAsync(newEntries);
        await db.SaveChangesAsync();
    }
}
