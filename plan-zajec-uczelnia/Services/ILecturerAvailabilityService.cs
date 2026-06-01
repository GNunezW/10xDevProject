namespace plan_zajec_uczelnia.Services;

public interface ILecturerAvailabilityService
{
    Task<HashSet<(DayOfWeek Day, int TimeSlotId)>> GetByLecturerAsync(int lecturerId);
    Task SaveAsync(int lecturerId, IEnumerable<(DayOfWeek Day, int TimeSlotId)> slots);
}
