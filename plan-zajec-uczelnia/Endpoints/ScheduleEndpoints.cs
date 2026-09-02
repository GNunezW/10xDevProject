using Microsoft.AspNetCore.Authorization;
using plan_zajec_uczelnia.Services.Scheduling;

namespace plan_zajec_uczelnia.Endpoints;

public static class ScheduleEndpoints
{
    private static readonly AuthorizeAttribute JwtOnly = new()
    {
        AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme
    };

    public static IEndpointRouteBuilder MapScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/scheduling/validate", async (
            IScheduleValidationService validation,
            CancellationToken ct) =>
        {
            var result = await validation.ValidateAsync(ct);
            return Results.Ok(result);
        })
        .WithName("ValidateSchedulingInputs")
        .RequireAuthorization(JwtOnly);

        app.MapPost("/api/scheduling/generate", async (
            IScheduleGenerationService generation,
            CancellationToken ct) =>
        {
            var run = await generation.GenerateAsync(ct);
            return Results.Ok(new { run.Id, run.Status, run.ErrorMessage });
        })
        .WithName("GenerateSchedule")
        .RequireAuthorization(JwtOnly);

        return app;
    }
}
