using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services;

public interface INonWorkingDayService
{
    Task<List<NonWorkingDay>> GetAllAsync();
    Task<NonWorkingDay> CreateAsync(NonWorkingDay day);
    Task UpdateAsync(NonWorkingDay day);
    Task DeleteAsync(int id);
}
