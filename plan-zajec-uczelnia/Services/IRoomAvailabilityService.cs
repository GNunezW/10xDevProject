namespace plan_zajec_uczelnia.Services;

public interface IRoomAvailabilityService
{
    Task<HashSet<(DayOfWeek Day, int TimeSlotId)>> GetByRoomAsync(string roomNumber);
    Task SaveAsync(string roomNumber, IEnumerable<(DayOfWeek Day, int TimeSlotId)> slots);
}
