namespace plan_zajec_uczelnia.Services.Scheduling;

public interface IScheduleValidationService
{
    Task<ScheduleValidationResult> ValidateAsync(CancellationToken cancellationToken = default);
}
