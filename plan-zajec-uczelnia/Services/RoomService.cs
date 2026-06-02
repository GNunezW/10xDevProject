using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Data;
using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services;

public class RoomService(ApplicationDbContext db) : IRoomService
{
    public Task<List<Room>> GetAllAsync() =>
        db.Rooms.AsNoTracking()
            .Include(r => r.InstructionType)
            .OrderBy(r => r.RoomNumber)
            .ToListAsync();

    public Task<Room?> GetByRoomNumberAsync(string roomNumber) =>
        db.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.RoomNumber == roomNumber);

    public async Task<Room> CreateAsync(Room room)
    {
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        return room;
    }

    public async Task UpdateAsync(Room room)
    {
        var existing = await db.Rooms.FindAsync(room.RoomNumber);
        if (existing is null) return;
        existing.InstructionTypeId = room.InstructionTypeId;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string roomNumber)
    {
        var room = await db.Rooms.FindAsync(roomNumber);
        if (room is not null)
        {
            db.Rooms.Remove(room);
            await db.SaveChangesAsync();
        }
    }
}
