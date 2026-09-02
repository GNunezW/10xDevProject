using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services.Scheduling;

public interface IScheduleViewService
{
    Task<ScheduleRun?> GetRunAsync(int runId, CancellationToken cancellationToken = default);
    Task<List<StudyProgram>> GetProgramsForRunAsync(int runId, CancellationToken cancellationToken = default);
    Task<List<ScheduledSession>> GetSessionsAsync(
        int runId,
        int studyProgramId,
        int? semester = null,
        CancellationToken cancellationToken = default);
    Task<List<int>> GetSemestersForProgramRunAsync(
        int runId,
        int studyProgramId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DayGapMetrics>> GetGapMetricsAsync(
        int runId,
        int studyProgramId,
        int? semester = null,
        CancellationToken cancellationToken = default);
    Task<(List<ScheduledSession> Sessions, IReadOnlyList<DayGapMetrics> Metrics, ScheduleGridData Grid)> GetProgramScheduleAsync(
        int runId,
        int studyProgramId,
        int? semester = null,
        CancellationToken cancellationToken = default);
    Task<List<ScheduleGroupSummary>> GetGroupSummariesAsync(
        int runId,
        int studyProgramId,
        int semester,
        CancellationToken cancellationToken = default);
    Task<List<ScheduleRun>> GetRecentRunsAsync(int count = 20, CancellationToken cancellationToken = default);
}
