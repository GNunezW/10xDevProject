using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services;

public interface ILecturerService
{
    Task<List<Lecturer>> GetAllAsync();
    Task<Lecturer?> GetByIdAsync(int id);
    Task<Lecturer> CreateAsync(Lecturer lecturer);
    Task UpdateAsync(Lecturer lecturer);
    Task DeleteAsync(int id);
}
