using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Data;
using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services.Scheduling;

public class ScheduleViewService(ApplicationDbContext db) : IScheduleViewService
{
    public Task<ScheduleRun?> GetRunAsync(int runId, CancellationToken cancellationToken = default) =>
        db.ScheduleRuns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);

    public Task<List<ScheduleRun>> GetRecentRunsAsync(int count = 20, CancellationToken cancellationToken = default) =>
        db.ScheduleRuns.AsNoTracking()
            .OrderByDescending(r => r.StartedAt)
            .Take(count)
            .ToListAsync(cancellationToken);

    public async Task<List<StudyProgram>> GetProgramsForRunAsync(int runId, CancellationToken cancellationToken = default)
    {
        var programIds = await db.ScheduledSessions.AsNoTracking()
            .Where(s => s.ScheduleRunId == runId)
            .Select(s => s.StudyProgramId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return await db.StudyPrograms.AsNoTracking()
            .Where(p => programIds.Contains(p.Id))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<List<ScheduledSession>> GetSessionsAsync(
        int runId,
        int studyProgramId,
        CancellationToken cancellationToken = default) =>
        db.ScheduledSessions.AsNoTracking()
            .Include(s => s.Subject)
            .Include(s => s.TimeSlot)
            .Include(s => s.Room)
            .Include(s => s.Lecturer)
            .Where(s => s.ScheduleRunId == runId && s.StudyProgramId == studyProgramId)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.TimeSlot.StartTime)
            .ThenBy(s => s.Subject.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DayGapMetrics>> GetGapMetricsAsync(
        int runId,
        int studyProgramId,
        CancellationToken cancellationToken = default)
    {
        var sessions = await GetSessionsAsync(runId, studyProgramId, cancellationToken);
        var slots = await db.TimeSlots.AsNoTracking().OrderBy(t => t.StartTime).ToListAsync(cancellationToken);
        var slotIndex = slots.Select((s, i) => (s.Id, i)).ToDictionary(x => x.Id, x => x.i);
        const int blockMinutes = 90;

        var metrics = new List<DayGapMetrics>();
        foreach (var dayGroup in sessions.GroupBy(s => s.DayOfWeek))
        {
            var usedIndices = dayGroup
                .Select(s => slotIndex.GetValueOrDefault(s.TimeSlotId, -1))
                .Where(i => i >= 0)
                .Distinct()
                .OrderBy(i => i)
                .ToList();

            if (usedIndices.Count <= 1)
            {
                metrics.Add(new DayGapMetrics(dayGroup.Key, 0, 0));
                continue;
            }

            var min = usedIndices.Min();
            var max = usedIndices.Max();
            var spanSlots = max - min + 1;
            var gapSlots = spanSlots - usedIndices.Count;
            metrics.Add(new DayGapMetrics(dayGroup.Key, gapSlots, gapSlots * blockMinutes));
        }

        return metrics.OrderBy(m => m.Day).ToList();
    }
}
