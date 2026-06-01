using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services;

public interface ISemesterPeriodService
{
    Task<List<SemesterPeriod>> GetAllAsync();
    Task<SemesterPeriod> CreateAsync(SemesterPeriod period);
    Task UpdateAsync(SemesterPeriod period);
    Task DeleteAsync(int id);
}
