using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Data;
using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services;

public class LecturerAvailabilityService(ApplicationDbContext db) : ILecturerAvailabilityService
{
    public async Task<HashSet<(DayOfWeek Day, int TimeSlotId)>> GetByLecturerAsync(int lecturerId)
    {
        var rows = await db.LecturerAvailabilities
            .AsNoTracking()
            .Where(la => la.LecturerId == lecturerId)
            .Select(la => new { la.DayOfWeek, la.TimeSlotId })
            .ToListAsync();

        return rows.Select(r => (r.DayOfWeek, r.TimeSlotId)).ToHashSet();
    }

    public async Task SaveAsync(int lecturerId, IEnumerable<(DayOfWeek Day, int TimeSlotId)> slots)
    {
        var existing = db.LecturerAvailabilities.Where(la => la.LecturerId == lecturerId);
        db.LecturerAvailabilities.RemoveRange(existing);

        var newEntries = slots.Select(s => new LecturerAvailability
        {
            LecturerId = lecturerId,
            DayOfWeek = s.Day,
            TimeSlotId = s.TimeSlotId
        });

        await db.LecturerAvailabilities.AddRangeAsync(newEntries);
        await db.SaveChangesAsync();
    }
}
