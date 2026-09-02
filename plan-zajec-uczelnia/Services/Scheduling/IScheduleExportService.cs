namespace plan_zajec_uczelnia.Services.Scheduling;

public interface IScheduleExportService
{
    Task<ScheduleExportFile?> ExportProgramAsync(
        int runId,
        int studyProgramId,
        CancellationToken cancellationToken = default);
}

public sealed class ScheduleExportFile
{
    public required byte[] Content { get; init; }
    public required string FileName { get; init; }

    public const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
