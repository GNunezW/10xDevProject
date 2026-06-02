using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services.Scheduling;

public interface IScheduleGenerationService
{
    Task<ScheduleRun> GenerateAsync(CancellationToken cancellationToken = default);
}
