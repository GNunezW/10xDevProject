using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using plan_zajec_uczelnia.Data;
using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services.Scheduling;

public static class SchedulePersistenceHelper
{
    private const int LegacyErrorMessageMaxLength = 2000;
    private const int ExpandedErrorMessageMaxLength = 8000;

    public static string TruncateErrorMessage(string message, int maxLength)
    {
        if (message.Length <= maxLength)
            return message;

        const string suffix = "\n… (komunikat skrócony)";
        var take = Math.Max(0, maxLength - suffix.Length);
        return message[..take] + suffix;
    }

    public static string FormatException(Exception ex) =>
        ex switch
        {
            DbUpdateException db when db.InnerException is not null =>
                $"{db.Message} {db.InnerException.Message}",
            _ => ex.Message
        };

    public static void DiscardPendingSessions(ApplicationDbContext db)
    {
        foreach (var entry in db.ChangeTracker.Entries<ScheduledSession>()
                     .Where(e => e.State == EntityState.Added)
                     .ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    public static async Task SaveFailedRunAsync(
        ApplicationDbContext db,
        ScheduleRun run,
        string message,
        CancellationToken ct,
        ILogger? logger = null)
    {
        DiscardPendingSessions(db);

        run.Status = ScheduleRunStatus.Failed;
        run.CompletedAt = DateTime.UtcNow;

        foreach (var maxLen in new[] { ExpandedErrorMessageMaxLength, LegacyErrorMessageMaxLength })
        {
            run.ErrorMessage = TruncateErrorMessage(message, maxLen);
            try
            {
                await db.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateException ex) when (maxLen > LegacyErrorMessageMaxLength)
            {
                logger?.LogWarning(
                    ex,
                    "Could not persist failed schedule run error at maxLen {MaxLen}; retrying shorter message.",
                    maxLen);
            }
        }

        run.ErrorMessage = TruncateErrorMessage(
            "Generowanie nie powiodło się. Uruchom: dotnet ef database update",
            LegacyErrorMessageMaxLength);
        await db.SaveChangesAsync(ct);
    }
}
