using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Data;
using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services.Scheduling;

internal static class SchedulePersistenceHelper
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

    public static async Task SaveFailedRunAsync(
        ApplicationDbContext db,
        ScheduleRun run,
        string message,
        CancellationToken ct)
    {
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
            catch (DbUpdateException) when (maxLen > LegacyErrorMessageMaxLength)
            {
                // Kolumna w bazie nadal varchar(2000) — spróbuj krótszej wersji.
            }
        }

        run.ErrorMessage = TruncateErrorMessage(
            "Generowanie nie powiodło się. Uruchom: dotnet ef database update",
            LegacyErrorMessageMaxLength);
        await db.SaveChangesAsync(ct);
    }
}
