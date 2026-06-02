using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services;

public interface IRoomService
{
    Task<List<Room>> GetAllAsync();
    Task<Room?> GetByRoomNumberAsync(string roomNumber);
    Task<Room> CreateAsync(Room room);
    Task UpdateAsync(Room room);
    Task DeleteAsync(string roomNumber);
}
