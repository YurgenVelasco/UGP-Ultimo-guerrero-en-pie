using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;

namespace UGP_ultimo_guerrero_en_pie_.Services.Administration;

public sealed class AdminSettings
{
    public const string Policy = "AdministracionUGP";
    public const string ExpiryClaim = "ugp.admin.expires";
    public const string MfaClaim = "ugp.admin.mfa";
    public string LoginPath { get; }
    public string? Role { get; }
    public bool RequireMfa { get; }
    public TimeSpan Lifetime => TimeSpan.FromMinutes(20);
    public AdminSettings(IConfiguration config, IHostEnvironment environment)
    {
        LoginPath = config["Administracion:RutaAcceso"] ?? "/acceso-ugp";
        // Reservar un segmento: evita colisiones con APIs y rutas existentes.
        if (!Regex.IsMatch(LoginPath, @"\A/acceso-[a-z0-9-]{3,60}\z"))
            throw new InvalidOperationException("Administracion:RutaAcceso debe ser /acceso- seguido de un identificador válido.");
        Role = config["Administracion:RolPermitido"]?.Trim();
        RequireMfa = !environment.IsDevelopment() || config.GetValue<bool>("Administracion:ExigirMfa");
    }
    public bool SessionValid(ClaimsPrincipal principal, DateTimeOffset now) =>
        principal.Identities.Any(i => i.IsAuthenticated && i.AuthenticationType == IdentityConstants.ApplicationScheme) &&
        long.TryParse(principal.FindFirstValue(ExpiryClaim), NumberStyles.None, CultureInfo.InvariantCulture, out var expires) &&
        expires > now.ToUnixTimeSeconds() && (!RequireMfa || principal.HasClaim(MfaClaim, "true"));
}

public sealed class AdminRequirement : IAuthorizationRequirement;

public sealed class AdminAuthorizationHandler(AdminSettings settings, SignInManager<ApplicationUser> signIn,
    UserManager<ApplicationUser> users, ILogger<AdminAuthorizationHandler> logger) : AuthorizationHandler<AdminRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        if (string.IsNullOrWhiteSpace(settings.Role) || !settings.SessionValid(context.User, DateTimeOffset.UtcNow)) return;
        try
        {
            // Revalidación en cada operación: revocar rol/stamp no espera a otra sesión.
            var user = await signIn.ValidateSecurityStampAsync(context.User);
            if (user is not null && user.LockoutEnabled && !await users.IsLockedOutAsync(user) &&
                (!settings.RequireMfa || await users.GetTwoFactorEnabledAsync(user)) &&
                await users.IsInRoleAsync(user, settings.Role)) context.Succeed(requirement);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Administración: autorización denegada por indisponibilidad ({Type}).", ex.GetType().Name);
        }
    }
}

// Toda futura operación privilegiada debe usar esta guardia, además de proteger su endpoint.
public sealed class AdminOperations(IAuthorizationService authorization, ILogger<AdminOperations> logger)
{
    public async Task<string> ReadStatusAsync(ClaimsPrincipal user)
    {
        await DemandAsync(user);
        logger.LogInformation("AdminAudit StatusRead Actor={Actor}", user.FindFirstValue(ClaimTypes.NameIdentifier));
        return "Panel privado. Gestión de eventos y catálogo de localidades con autorización del servidor.";
    }
    public async Task DemandAsync(ClaimsPrincipal user)
    {
        if (!(await authorization.AuthorizeAsync(user, null, AdminSettings.Policy)).Succeeded)
        {
            logger.LogWarning("AdminAudit OperationDenied Actor={Actor}", user.FindFirstValue(ClaimTypes.NameIdentifier));
            throw new UnauthorizedAccessException("Acceso administrativo no autorizado.");
        }
    }
}
