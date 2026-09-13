using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using plan_zajec_uczelnia.Data;

namespace plan_zajec_uczelnia.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var account = app.MapGroup("/account").AllowAnonymous();

        account.MapPost("/login", async (
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            [FromForm] string email,
            [FromForm] string password,
            [FromForm] string? returnUrl) =>
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
                return Results.Redirect(BuildLoginRedirect("invalid", returnUrl));

            var result = await signInManager.PasswordSignInAsync(
                user, password, isPersistent: true, lockoutOnFailure: true);

            if (result.IsLockedOut)
                return Results.Redirect(BuildLoginRedirect("locked", returnUrl));

            if (!result.Succeeded)
                return Results.Redirect(BuildLoginRedirect("invalid", returnUrl));

            var target = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
            return Results.Redirect(target);
        }).DisableAntiforgery();

        account.MapPost("/logout", async (SignInManager<AppUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.Redirect("/login");
        }).DisableAntiforgery();
    }

    private static string BuildLoginRedirect(string error, string? returnUrl)
    {
        var query = $"?error={error}";
        if (!string.IsNullOrWhiteSpace(returnUrl))
            query += $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
        return $"/login{query}";
    }
}
