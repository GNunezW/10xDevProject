using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using plan_zajec_uczelnia.Services.Scheduling;

namespace plan_zajec_uczelnia.Endpoints;

public static class ScheduleExportEndpoints
{
    private static readonly AuthorizeAttribute CookieOnly = new()
    {
        AuthenticationSchemes = IdentityConstants.ApplicationScheme
    };

    public static IEndpointRouteBuilder MapScheduleExportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/plan/{runId:int}/eksport", async (
            int runId,
            int programId,
            IScheduleExportService export,
            CancellationToken ct) =>
        {
            var file = await export.ExportProgramAsync(runId, programId, ct);
            if (file is null)
                return Results.NotFound();

            return Results.File(file.Content, ScheduleExportFile.ExcelContentType, file.FileName);
        })
        .WithName("ExportScheduleProgram")
        .RequireAuthorization(CookieOnly);

        return app;
    }
}
