namespace plan_zajec_uczelnia.Services.Scheduling;

public sealed record DayGapMetrics(DayOfWeek Day, int GapCount, int GapMinutes);
