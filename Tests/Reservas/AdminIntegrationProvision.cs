using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Claims;
using UGP_ultimo_guerrero_en_pie_.Data;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Herramienta manual del responsable. Nunca se carga/ejecuta en el arranque de UGP.
internal static class AdminIntegrationProvision
{
    private const string Marker = "ugp.integration.admin621";
    internal static string Stage { get; private set; } = "precondiciones";
    public static async Task Run()
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
            throw new InvalidOperationException("Ejecutar únicamente en una consola local interactiva del responsable, sin redirección ni transcripción.");
        if (!await AdminIntegrationPreflight.Run()) return;
        Console.WriteLine("Destino único: UGP_Integracion_Test. No se configura producción ni se modifica DefaultConnection.");
        Console.Write("Rol administrativo autorizado (se usará como Administracion:RolPermitido en el host aislado): ");
        var role = Console.ReadLine()?.Trim();
        Console.Write("Nombre de la única cuenta de integración: ");
        var name = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(role) || role.Length > 100 || role.Any(char.IsControl) ||
            string.IsNullOrWhiteSpace(name) || name.Length > 256) throw new InvalidOperationException("Entrada inválida.");
        var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json", true)
            .AddUserSecrets("UGP-ultimo-guerrero-en-pie").AddEnvironmentVariables().Build();
        var configuredRole = config["Administracion:RolPermitido"]?.Trim();
        if (!string.IsNullOrEmpty(configuredRole) && !string.Equals(role, configuredRole, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El rol no coincide con la política configurada. No se modifica configuración.");
        Console.Write("Escribe UGP_Integracion_Test para confirmar el destino: ");
        if (Console.ReadLine() != "UGP_Integracion_Test") return;
        var services = new ServiceCollection().AddLogging();
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(AdminIntegrationPreflight.Connection));
        services.AddIdentityCore<ApplicationUser>(o =>
        {
            // Restricción de provisión de esta cuenta de prueba; no cambia opciones de UGP.
            o.Password.RequiredLength = 12; o.Password.RequireDigit = true;
            o.Password.RequireLowercase = true; o.Password.RequireUppercase = true;
            o.Password.RequireNonAlphanumeric = true;
        }).AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var user = await users.FindByNameAsync(name);
        if (user is not null)
        {
            var claims = await users.GetClaimsAsync(user);
            if (!claims.Any(c => c.Type == Marker && c.Value == role) || !await users.IsInRoleAsync(user, role))
                throw new InvalidOperationException("La cuenta existente no pertenece a este provisionamiento. No se modifica.");
            Console.WriteLine("Cuenta ya provisionada: no se cambian contraseña, MFA, bloqueo ni asignaciones.");
            await transaction.RollbackAsync();
            return;
        }
        if (await users.Users.AnyAsync())
            throw new InvalidOperationException("La base ya contiene otra cuenta. Detenerse para revisión; no crear otra automáticamente.");
        Console.Write("Contraseña nueva (entrada oculta): ");
        var password = ReadHidden();
        Console.Write("Repetir contraseña (entrada oculta): ");
        var confirmation = ReadHidden();
        if (password != confirmation)
        {
            Console.WriteLine("Las dos contraseñas no coinciden. No se crea la cuenta ni el rol. Repite la operación.");
            Environment.ExitCode = 2;
            return;
        }
        Stage = "crear rol mediante Identity";
        if (!await roles.RoleExistsAsync(role)) Require(await roles.CreateAsync(new IdentityRole(role)));
        user = new ApplicationUser { UserName = name, LockoutEnabled = true };
        Stage = "validar y crear usuario mediante Identity";
        Require(await users.CreateAsync(user, password));
        password = confirmation = string.Empty;
        Stage = "asignar rol";
        Require(await users.AddToRoleAsync(user, role));
        Stage = "registrar marca de provisionamiento";
        Require(await users.AddClaimAsync(user, new Claim(Marker, role)));
        Stage = "confirmar transacción";
        await transaction.CommitAsync();
        Console.WriteLine("Cuenta creada mediante Identity; una asignación. Contraseña no registrada. MFA pendiente: no usar fuera del host Development aislado hasta completar inscripción y pruebas.");
    }

    internal static string ReadHidden()
    {
        var input = new List<char>();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Escape) throw new OperationCanceledException();
            if (key.Key == ConsoleKey.Backspace) { if (input.Count > 0) input.RemoveAt(input.Count - 1); }
            else if (!char.IsControl(key.KeyChar) && input.Count < 256) input.Add(key.KeyChar);
        }
        Console.WriteLine();
        var result = new string(input.ToArray());
        for (var i = 0; i < input.Count; i++) input[i] = '\0';
        return result;
    }
    internal static void Require(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            // Sólo mensajes constantes: nunca Description, valores de entrada ni excepciones internas.
            foreach (var error in result.Errors)
                Console.WriteLine(error.Code switch
                {
                    "PasswordTooShort" => "Identity: la contraseña requiere al menos 12 caracteres.",
                    "PasswordRequiresDigit" => "Identity: falta un número en la contraseña.",
                    "PasswordRequiresLower" => "Identity: falta una letra minúscula.",
                    "PasswordRequiresUpper" => "Identity: falta una letra mayúscula.",
                    "PasswordRequiresNonAlphanumeric" => "Identity: falta un símbolo.",
                    "PasswordRequiresUniqueChars" => "Identity: faltan caracteres distintos.",
                    "DuplicateUserName" => "Identity: ya existe ese nombre de usuario.",
                    "InvalidUserName" => "Identity: el formato del nombre de usuario no es válido.",
                    "DuplicateRoleName" => "Identity: ya existe ese nombre de rol.",
                    "InvalidRoleName" => "Identity: el formato del nombre de rol no es válido.",
                    "ConcurrencyFailure" => "Identity: hubo una modificación concurrente; no reintentar sin verificar estado.",
                    _ => "Identity rechazó la operación; requiere revisión sin mostrar datos privados."
                });
            throw new InvalidOperationException("Rechazo Identity.");
        }
    }
}
