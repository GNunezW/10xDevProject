using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Data;
using plan_zajec_uczelnia.Models;
using plan_zajec_uczelnia.Services.Scheduling;

namespace plan_zajec_uczelnia.Tests;

/// <summary>
/// Risk #3 / M3L5 debug-as-test: a failed generate must not leave a saved plan.
/// Repro: sessions are already Added on the context, then FailRun persists Failed
/// and the pending sessions ride along on the same SaveChanges.
/// </summary>
public class SchedulePersistOnFailureTests
{
    [Fact]
    public async Task Failed_run_does_not_keep_sessions_that_were_pending_on_the_context()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"persist-on-fail-{Guid.NewGuid()}")
            .Options;

        await using var db = new ApplicationDbContext(options);
        var ct = TestContext.Current.CancellationToken;
        await db.Database.EnsureCreatedAsync(ct);

        var run = new ScheduleRun
        {
            StartedAt = DateTime.UtcNow,
            Status = ScheduleRunStatus.Running,
            SolverTimeLimitSeconds = 60
        };
        db.ScheduleRuns.Add(run);
        await db.SaveChangesAsync(ct);

        db.ScheduledSessions.Add(new ScheduledSession
        {
            ScheduleRunId = run.Id,
            StudyProgramId = 1,
            SubjectId = 1,
            GroupIndex = 1,
            SessionIndex = 1,
            DayOfWeek = DayOfWeek.Monday,
            TimeSlotId = 1,
            RoomNumber = "A1",
            LecturerId = 1
        });

        await SchedulePersistenceHelper.SaveFailedRunAsync(
            db, run, "zapis planu nie powiódł się", ct);

        Assert.Equal(ScheduleRunStatus.Failed, run.Status);
        Assert.Empty(db.ScheduledSessions.AsNoTracking().ToList());
    }
}
