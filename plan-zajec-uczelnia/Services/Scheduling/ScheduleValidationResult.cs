namespace plan_zajec_uczelnia.Services.Scheduling;

public sealed record ScheduleValidationResult(bool CanRun, IReadOnlyList<string> Issues);
