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
        int? semester = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.ScheduledSessions.AsNoTracking()
            .Include(s => s.Subject)
            .Include(s => s.TimeSlot)
            .Include(s => s.Room)
            .Include(s => s.Lecturer)
            .Where(s => s.ScheduleRunId == runId && s.StudyProgramId == studyProgramId);

        if (semester is not null)
            query = query.Where(s => s.Subject!.Semester == semester.Value);

        return query
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.TimeSlot.StartTime)
            .ThenBy(s => s.Subject!.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<List<int>> GetSemestersForProgramRunAsync(
        int runId,
        int studyProgramId,
        CancellationToken cancellationToken = default) =>
        db.ScheduledSessions.AsNoTracking()
            .Where(s => s.ScheduleRunId == runId && s.StudyProgramId == studyProgramId)
            .Select(s => s.Subject!.Semester)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(cancellationToken);

    public async Task<(List<ScheduledSession> Sessions, IReadOnlyList<DayGapMetrics> Metrics, ScheduleGridData Grid)> GetProgramScheduleAsync(
        int runId,
        int studyProgramId,
        int? semester = null,
        CancellationToken cancellationToken = default)
    {
        var sessions = await GetSessionsAsync(runId, studyProgramId, semester, cancellationToken);
        var metrics = await ComputeGapMetricsAsync(sessions, cancellationToken);
        var grid = await BuildGridAsync(sessions, studyProgramId, cancellationToken);
        return (sessions, metrics, grid);
    }

    private async Task<ScheduleGridData> BuildGridAsync(
        List<ScheduledSession> sessions,
        int studyProgramId,
        CancellationToken cancellationToken)
    {
        var program = await db.StudyPrograms.AsNoTracking()
            .FirstAsync(p => p.Id == studyProgramId, cancellationToken);
        var timeSlots = await db.TimeSlots.AsNoTracking().OrderBy(t => t.StartTime).ToListAsync(cancellationToken);
        var days = ScheduleGridData.GetDaysForMode(program.StudyMode);

        var cells = sessions
            .GroupBy(s => new ScheduleGridCellKey(s.DayOfWeek, s.TimeSlotId))
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ScheduledSession>)g
                    .OrderBy(s => s.Subject?.Semester)
                    .ThenBy(s => s.Subject?.Name)
                    .ThenBy(s => s.GroupIndex)
                    .ThenBy(s => s.SessionIndex)
                    .ToList());

        return new ScheduleGridData
        {
            TimeSlots = timeSlots,
            Days = days,
            Cells = cells
        };
    }

    public async Task<IReadOnlyList<DayGapMetrics>> GetGapMetricsAsync(
        int runId,
        int studyProgramId,
        int? semester = null,
        CancellationToken cancellationToken = default)
    {
        var sessions = await GetSessionsAsync(runId, studyProgramId, semester, cancellationToken);
        return await ComputeGapMetricsAsync(sessions, cancellationToken);
    }

    private async Task<IReadOnlyList<DayGapMetrics>> ComputeGapMetricsAsync(
        List<ScheduledSession> sessions,
        CancellationToken cancellationToken)
    {
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

    public async Task<List<ScheduleGroupSummary>> GetGroupSummariesAsync(
        int runId,
        int studyProgramId,
        int semester,
        CancellationToken cancellationToken = default)
    {
        var program = await db.StudyPrograms.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == studyProgramId, cancellationToken);
        if (program is null)
            return [];

        var enrollment = await db.StudyProgramEnrollments.AsNoTracking()
            .FirstOrDefaultAsync(e => e.StudyProgramId == studyProgramId && e.Semester == semester, cancellationToken);

        var subjects = await db.Subjects.AsNoTracking()
            .Include(s => s.InstructionType)
            .Where(s => s.StudyProgramId == studyProgramId && s.Semester == semester)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        var periods = await db.SemesterPeriods.AsNoTracking()
            .Where(p => p.AcademicYear == program.AcademicYear)
            .ToListAsync(cancellationToken);

        var nonWorkingDayRows = await db.NonWorkingDays.AsNoTracking().ToListAsync(cancellationToken);
        var holidays = ScheduleSemesterBounds.ResolveNonWorkingDays(program.AcademicYear, nonWorkingDayRows);
        var (semesterStart, semesterEnd) = ScheduleSemesterBounds.Resolve(program.AcademicYear, periods);

        var sessions = await GetSessionsAsync(runId, studyProgramId, semester, cancellationToken);
        var sessionLookup = sessions.ToDictionary(
            s => (s.SubjectId, s.GroupIndex, s.SessionIndex));

        var studentCount = enrollment?.StudentCount ?? 0;
        var summaries = new List<ScheduleGroupSummary>();

        foreach (var subject in subjects)
        {
            if (subject.InstructionType is null || studentCount <= 0 || subject.NumberOfSessions <= 0)
                continue;

            var groupCount = InstructionType.EstimateGroupCount(studentCount, subject.InstructionType);
            if (groupCount <= 0)
                continue;

            if (semesterStart is null || semesterEnd is null)
                continue;

            var weeklySlots = ScheduleWeekCalculator.ResolveWeeklySlotCount(
                subject.NumberOfSessions,
                semesterStart.Value,
                semesterEnd.Value,
                program.StudyMode,
                holidays);
            var lastSessionIndex = weeklySlots <= 0
                ? 0
                : ((subject.NumberOfSessions - 1) % weeklySlots) + 1;

            for (var group = 1; group <= groupCount; group++)
            {
                DateOnly? lastDate = null;
                if (lastSessionIndex > 0
                    && sessionLookup.TryGetValue((subject.Id, group, lastSessionIndex), out var lastTemplate))
                {
                    lastDate = ScheduleCalendarHelper.ResolveLastMeetingDate(
                        semesterStart.Value,
                        semesterEnd.Value,
                        subject.NumberOfSessions,
                        weeklySlots,
                        lastTemplate.DayOfWeek,
                        holidays);
                }

                summaries.Add(new ScheduleGroupSummary
                {
                    SubjectName = subject.Name,
                    GroupIndex = group,
                    MeetingsInSemester = subject.NumberOfSessions,
                    WeeklySlots = weeklySlots,
                    LastSessionDate = lastDate
                });
            }
        }

        return summaries;
    }
}
