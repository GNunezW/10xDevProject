using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MudBlazor.Services;
using plan_zajec_uczelnia.Components;
using plan_zajec_uczelnia.Data;
using plan_zajec_uczelnia.Data.Seeding;
using plan_zajec_uczelnia.Endpoints;
using plan_zajec_uczelnia.Models;
using plan_zajec_uczelnia.Services;
using plan_zajec_uczelnia.Services.Scheduling;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.Cookie.HttpOnly = true;
});

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured in user-secrets.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization(options =>
{
    // Blazor UI: cookie Identity. API: osobno JwtBearer na endpointach.
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(
            IdentityConstants.ApplicationScheme)
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();

builder.Services.AddScoped<IStudyProgramService, StudyProgramService>();
builder.Services.AddScoped<ILecturerService, LecturerService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<ITimeSlotService, TimeSlotService>();
builder.Services.AddScoped<ISemesterPeriodService, SemesterPeriodService>();
builder.Services.AddScoped<INonWorkingDayService, NonWorkingDayService>();
builder.Services.AddScoped<ILecturerAvailabilityService, LecturerAvailabilityService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IInstructionTypeService, InstructionTypeService>();
builder.Services.AddScoped<IStudyProgramEnrollmentService, StudyProgramEnrollmentService>();
builder.Services.AddScoped<ISubjectStaffingService, SubjectStaffingService>();
builder.Services.AddScoped<IScheduleValidationService, ScheduleValidationService>();
builder.Services.AddScoped<IScheduleGenerationService, ScheduleGenerationService>();
builder.Services.AddScoped<IScheduleViewService, ScheduleViewService>();
builder.Services.AddScoped<IScheduleExportService, ScheduleExportService>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/health"))
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync("ok");
        return;
    }

    await next();
});

app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        try
        {
            await using var migrateScope = app.Services.CreateAsyncScope();
            var db = migrateScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();
            await SeedCoordinatorAsync(app);
            if (app.Environment.IsDevelopment())
            {
                await ChemicalTechnologySeed.SeedAsync(app.Services);
                await ComputerScienceSeed.SeedAsync(app.Services);
            }

            await CleanupStaleScheduleRunsAsync(app);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Startup database migrate/seed failed; site is still listening.");
        }
    });
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true || IsAnonymousPath(context.Request.Path))
    {
        await next();
        return;
    }

    var returnUrl = Uri.EscapeDataString(
        context.Request.Path + context.Request.QueryString);
    context.Response.Redirect($"/login?ReturnUrl={returnUrl}");
});

app.MapAuthEndpoints();
app.MapAccountEndpoints();
app.MapScheduleEndpoints();
app.MapScheduleExportEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AllowAnonymous();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/health", () => Results.Text("ok")).AllowAnonymous();

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.AllowAnonymous();

app.Run();

static bool IsAnonymousPath(PathString path) =>
    path.StartsWithSegments("/login")
    || path.StartsWithSegments("/account")
    || path.StartsWithSegments("/auth")
    || path.StartsWithSegments("/_blazor")
    || path.StartsWithSegments("/_framework")
    || path.StartsWithSegments("/_content")
    || path.StartsWithSegments("/weatherforecast")
    || path.StartsWithSegments("/health")
    || path.StartsWithSegments("/.well-known");

static async Task SeedCoordinatorAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    var email = config["Seed:CoordinatorEmail"];
    var password = config["Seed:CoordinatorPassword"];

    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        return;

    var user = await userManager.FindByEmailAsync(email);
    if (user is null)
    {
        user = new AppUser { UserName = email, Email = email, EmailConfirmed = true };
        await userManager.CreateAsync(user, password);
        return;
    }

    if (!app.Environment.IsDevelopment())
        return;

    await userManager.ResetAccessFailedCountAsync(user);
    var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
    await userManager.ResetPasswordAsync(user, resetToken, password);
}

static async Task CleanupStaleScheduleRunsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var cutoff = DateTime.UtcNow.AddMinutes(-5);

    var staleRuns = await db.ScheduleRuns
        .Where(r => r.Status == ScheduleRunStatus.Running && r.StartedAt < cutoff)
        .ToListAsync();

    if (staleRuns.Count == 0)
        return;

    foreach (var run in staleRuns)
    {
        run.Status = ScheduleRunStatus.Failed;
        run.CompletedAt = DateTime.UtcNow;
        run.ErrorMessage = "Przerwano — poprzednie uruchomienie nie zakończyło się (np. restart aplikacji).";
    }

    await db.SaveChangesAsync();
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
