using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Data;
using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services.Scheduling;

public sealed class ScheduleExportService(
    ApplicationDbContext db,
    IScheduleViewService view) : IScheduleExportService
{
    public async Task<ScheduleExportFile?> ExportProgramAsync(
        int runId,
        int studyProgramId,
        CancellationToken cancellationToken = default)
    {
        var run = await view.GetRunAsync(runId, cancellationToken);
        if (run is null || run.Status != ScheduleRunStatus.Succeeded)
            return null;

        var program = await db.StudyPrograms.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == studyProgramId, cancellationToken);
        if (program is null)
            return null;

        var sessions = await view.GetSessionsAsync(runId, studyProgramId, semester: null, cancellationToken);
        if (sessions.Count == 0)
            return null;

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SanitizeSheetName(program.Name));
        WriteHeader(sheet);
        WriteRows(sheet, sessions);
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ScheduleExportFile
        {
            Content = stream.ToArray(),
            FileName = $"plan-{SanitizeFileName(program.Name)}-{runId}.xlsx"
        };
    }

    private static void WriteHeader(IXLWorksheet sheet)
    {
        string[] headers =
        [
            "Dzień",
            "Godzina",
            "Przedmiot",
            "Semestr",
            "Grupa",
            "Blok",
            "Sala",
            "Prowadzący"
        ];

        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        sheet.Row(1).Style.Font.Bold = true;
    }

    private static void WriteRows(IXLWorksheet sheet, List<ScheduledSession> sessions)
    {
        var row = 2;
        foreach (var session in sessions)
        {
            var start = session.TimeSlot.StartTime;
            var end = start.Add(TimeSpan.FromMinutes(90));
            var lecturer = session.Lecturer is null
                ? "—"
                : $"{session.Lecturer.FirstName} {session.Lecturer.LastName}";

            sheet.Cell(row, 1).Value = DayLabel(session.DayOfWeek);
            sheet.Cell(row, 2).Value = $"{start.ToString("HH:mm")}–{end.ToString("HH:mm")}";
            sheet.Cell(row, 3).Value = session.Subject?.Name ?? "—";
            sheet.Cell(row, 4).Value = session.Subject?.Semester ?? 0;
            sheet.Cell(row, 5).Value = session.GroupIndex;
            sheet.Cell(row, 6).Value = session.SessionIndex;
            sheet.Cell(row, 7).Value = session.RoomNumber;
            sheet.Cell(row, 8).Value = lecturer;
            row++;
        }
    }

    private static string DayLabel(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Poniedziałek",
        DayOfWeek.Tuesday => "Wtorek",
        DayOfWeek.Wednesday => "Środa",
        DayOfWeek.Thursday => "Czwartek",
        DayOfWeek.Friday => "Piątek",
        DayOfWeek.Saturday => "Sobota",
        DayOfWeek.Sunday => "Niedziela",
        _ => day.ToString()
    };

    private static string SanitizeSheetName(string name)
    {
        var cleaned = string.Concat(name.Where(c => c is not (':' or '\\' or '/' or '?' or '*' or '[' or ']')));
        if (string.IsNullOrWhiteSpace(cleaned))
            cleaned = "Plan";
        return cleaned.Length <= 31 ? cleaned : cleaned[..31];
    }

    private static string SanitizeFileName(string name)
    {
        var cleaned = string.Concat(name.Select(c => char.IsLetterOrDigit(c) ? c : '-'));
        cleaned = cleaned.Trim('-');
        if (string.IsNullOrEmpty(cleaned))
            cleaned = "kierunek";
        return cleaned;
    }
}
