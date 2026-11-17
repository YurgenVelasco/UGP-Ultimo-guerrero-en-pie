using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UGP_ultimo_guerrero_en_pie_.Data;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;

internal static class AdminIntegrationMfa
{
    public static async Task Run()
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
            throw new InvalidOperationException("Sólo consola local interactiva sin transcripción.");
        if (!await AdminIntegrationPreflight.Run()) return;
        Console.Write("Usuario de integración: ");
        var name = Console.ReadLine()?.Trim();
        var services = new ServiceCollection().AddLogging();
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(AdminIntegrationPreflight.Connection));
        services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = string.IsNullOrEmpty(name) ? null : await users.FindByNameAsync(name);
        if (user is null || !(await users.GetClaimsAsync(user)).Any(c => c.Type == "ugp.integration.admin621") || await users.IsLockedOutAsync(user))
            throw new InvalidOperationException("Cuenta no válida para esta herramienta.");
        Console.Write("Contraseña de la cuenta (oculta): ");
        if (!await users.CheckPasswordAsync(user, AdminIntegrationProvision.ReadHidden()))
        {
            AdminIntegrationProvision.Require(await users.AccessFailedAsync(user));
            throw new InvalidOperationException("No autorizado.");
        }
        if (await users.GetTwoFactorEnabledAsync(user))
        {
            Console.WriteLine("MFA ya está habilitado. No se rota la clave ni se regeneran códigos.");
            return;
        }
        var key = await users.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            AdminIntegrationProvision.Require(await users.ResetAuthenticatorKeyAsync(user));
            key = await users.GetAuthenticatorKeyAsync(user);
        }
        Console.Clear();
        Console.WriteLine("PRIVADO: sólo pantalla local, no capturar ni compartir. Google Authenticator > Añadir > Introducir clave de configuración > Basada en tiempo.");
        Console.WriteLine("Cuenta: UGP integración. Clave: " + key);
        Console.Write("Código de Google Authenticator (entrada oculta): ");
        var code = AdminIntegrationProvision.ReadHidden().Replace(" ", "");
        Console.Clear();
        if (!await users.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, code))
            throw new InvalidOperationException("Código no válido; MFA no se activó.");
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        AdminIntegrationProvision.Require(await users.SetTwoFactorEnabledAsync(user, true));
        AdminIntegrationProvision.Require(await users.UpdateSecurityStampAsync(user));
        await transaction.CommitAsync();
        Console.WriteLine("MFA activado tras verificar un TOTP auténtico de tu aplicación. Sesiones anteriores revocadas por stamp.");
        // No generar códigos que la aplicación aún no acepta en su login.
        Console.WriteLine("Recuperación: pendiente de un mecanismo aprobado; no se generan ni muestran códigos inutilizables.");
    }
}
