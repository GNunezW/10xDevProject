using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services;

public interface ITimeSlotService
{
    Task<List<TimeSlot>> GetAllAsync();
    Task<TimeSlot> CreateAsync(TimeSlot slot);
    Task DeleteAsync(int id);
}
