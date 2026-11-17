using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using UGP_ultimo_guerrero_en_pie_.Components.Administration;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;

namespace UGP_ultimo_guerrero_en_pie_.Services.Administration;

public static class AdminEndpoints
{
    public static void MapAdminAccess(this WebApplication app)
    {
        var settings = app.Services.GetRequiredService<AdminSettings>();
        app.MapGet(settings.LoginPath, (HttpContext context) => LoginPage(context, settings, false))
            .AllowAnonymous().RequireRateLimiting("admin-login");
        app.MapPost(settings.LoginPath, LoginAsync).AllowAnonymous().RequireRateLimiting("admin-login");
        app.MapPost("/administracion/salir", async (HttpContext context, IAntiforgery antiforgery,
            SignInManager<ApplicationUser> signIn, ILogger<AdminOperations> log) =>
        {
            if (!context.Request.IsHttps) return Results.StatusCode(400);
            if (!await antiforgery.IsRequestValidAsync(context)) return Results.StatusCode(400);
            log.LogInformation("AdminAudit Logout Actor={Actor}", context.User.FindFirstValue(ClaimTypes.NameIdentifier));
            await signIn.SignOutAsync();
            return Results.LocalRedirect(settings.LoginPath);
        }).RequireAuthorization();
        app.MapGet("/api/administracion/estado", async (HttpContext context, AdminOperations operations) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(new { estado = await operations.ReadStatusAsync(context.User) });
        }).RequireAuthorization(AdminSettings.Policy);
    }

    private static IResult LoginPage(HttpContext context, AdminSettings settings, bool error)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'self'; form-action 'self'; frame-ancestors 'none'; base-uri 'none'";
        return new RazorComponentResult<AdminLogin>(new { Action = context.Request.PathBase + settings.LoginPath, Error = error });
    }

    private static async Task<IResult> LoginAsync(HttpContext context, AdminSettings settings,
        IAntiforgery antiforgery, UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn,
        ILogger<AdminOperations> log)
    {
        if (!context.Request.IsHttps) return Results.StatusCode(400);
        var bodyLimit = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
        if (bodyLimit is { IsReadOnly: false }) bodyLimit.MaxRequestBodySize = 8192;
        if (context.Request.ContentLength > 8192) return Results.StatusCode(413);
        if (!context.Request.HasFormContentType || !await antiforgery.IsRequestValidAsync(context)) return Results.StatusCode(400);
        var form = await context.Request.ReadFormAsync();
        var name = form["usuario"].ToString().Trim();
        var password = form["clave"].ToString();
        var code = form["codigo"].ToString().Replace(" ", "").Replace("-", "");
        if (string.IsNullOrWhiteSpace(settings.Role) || name.Length is < 1 or > 256 || password.Length is < 1 or > 256 || code.Length > 10)
            return Denied();
        try
        {
            var user = await users.FindByNameAsync(name);
            if (user is null || !user.LockoutEnabled) return Denied();
            var checkedPassword = await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
            if (!checkedPassword.Succeeded || !await users.IsInRoleAsync(user, settings.Role)) return Denied();
            var hasMfa = await users.GetTwoFactorEnabledAsync(user);
            if (settings.RequireMfa && !hasMfa) return Denied();
            if (hasMfa)
            {
                if (!await users.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, code))
                {
                    await users.AccessFailedAsync(user);
                    return Denied();
                }
                await users.ResetAccessFailedCountAsync(user);
            }
            // Sin sesión persistente ni retorno controlado por el navegador.
            var until = DateTimeOffset.UtcNow.Add(settings.Lifetime);
            await signIn.SignInWithClaimsAsync(user, new AuthenticationProperties
            {
                IsPersistent = false, AllowRefresh = false, ExpiresUtc = until
            }, new[]
            {
                new Claim(AdminSettings.ExpiryClaim, until.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)),
                new Claim(AdminSettings.MfaClaim, hasMfa ? "true" : "false")
            });
            log.LogInformation("AdminAudit LoginSucceeded Actor={Actor} Mfa={Mfa}", user.Id, hasMfa);
            return Results.LocalRedirect(context.Request.PathBase + "/administracion");
        }
        catch (Exception ex)
        {
            log.LogWarning("AdminAudit LoginUnavailable Type={Type}", ex.GetType().Name);
            return Denied();
        }
        IResult Denied()
        {
            log.LogWarning("AdminAudit LoginDenied Trace={Trace}", context.TraceIdentifier);
            return LoginPage(context, settings, true);
        }
    }
}
