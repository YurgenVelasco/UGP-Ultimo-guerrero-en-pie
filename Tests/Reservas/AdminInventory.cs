using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UGP_ultimo_guerrero_en_pie_.Data;

internal static class AdminInventory
{
    public static async Task Run()
    {
        var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json", true)
            .AddUserSecrets("UGP-ultimo-guerrero-en-pie").AddEnvironmentVariables().Build();
        try
        {
            var connection = config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connection)) { Console.WriteLine("INVENTARIO BLOQUEADO: falta conexión privada."); return; }
            await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connection, o => o.CommandTimeout(10)).Options);
            Console.WriteLine("Usuarios Identity: " + await db.Users.CountAsync());
            Console.WriteLine("Usuarios con MFA: " + await db.Users.CountAsync(u => u.TwoFactorEnabled));
            Console.WriteLine("Asignaciones de roles: " + await db.UserRoles.CountAsync());
            foreach (var role in await db.Roles.AsNoTracking().Select(r => r.Name).ToListAsync())
                Console.WriteLine("Rol existente: " + role);
            Console.WriteLine("Lectura agregada; no se consultaron contraseñas ni se modificaron cuentas.");
        }
        catch (Exception ex) { Console.WriteLine("INVENTARIO BLOQUEADO: " + ex.GetType().Name + ". Sin datos privados ni escrituras."); }
    }
}
