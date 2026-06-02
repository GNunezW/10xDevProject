using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services.Scheduling;

public interface IScheduleViewService
{
    Task<ScheduleRun?> GetRunAsync(int runId, CancellationToken cancellationToken = default);
    Task<List<StudyProgram>> GetProgramsForRunAsync(int runId, CancellationToken cancellationToken = default);
    Task<List<ScheduledSession>> GetSessionsAsync(int runId, int studyProgramId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DayGapMetrics>> GetGapMetricsAsync(int runId, int studyProgramId, CancellationToken cancellationToken = default);
    Task<List<ScheduleRun>> GetRecentRunsAsync(int count = 20, CancellationToken cancellationToken = default);
}
