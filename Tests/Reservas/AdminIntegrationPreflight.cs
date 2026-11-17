using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using UGP_ultimo_guerrero_en_pie_.Data;

internal static class AdminIntegrationPreflight
{
    // Destino literal: nunca utiliza DefaultConnection ni admite un servidor alternativo.
    internal const string Connection = @"Server=(localdb)\MSSQLLocalDB;Database=UGP_Integracion_Test;Integrated Security=true;Encrypt=false;Connect Timeout=10";

    public static async Task<bool> Run()
    {
        try
        {
            await using var connection = new SqlConnection(Connection);
            await connection.OpenAsync();
            using var identity = connection.CreateCommand();
            identity.CommandText = "SELECT DB_NAME(), CONVERT(int, SERVERPROPERTY('IsLocalDB'))";
            await using (var reader = await identity.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync() || reader.GetString(0) != "UGP_Integracion_Test" || reader.GetInt32(1) != 1)
                    throw new InvalidOperationException("Destino no autorizado.");
            }
            Console.WriteLine(@"Destino verificado: (localdb)\MSSQLLocalDB / UGP_Integracion_Test; autenticación Windows.");
            await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(Connection).Options);
            var columns = new Dictionary<string, (string Type, bool Nullable)>();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT t.name, c.name,
                  ty.name + CASE WHEN ty.name IN ('nvarchar','nchar') THEN '(' + CASE WHEN c.max_length=-1 THEN 'max' ELSE CONVERT(varchar(10),c.max_length/2) END + ')'
                                 WHEN ty.name IN ('varchar','char','varbinary','binary') THEN '(' + CASE WHEN c.max_length=-1 THEN 'max' ELSE CONVERT(varchar(10),c.max_length) END + ')'
                                 ELSE '' END, c.is_nullable
                FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
                JOIN sys.columns c ON c.object_id=t.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id
                WHERE s.name='dbo' AND t.name LIKE 'AspNet%'
                """;
            await using (var reader = await command.ExecuteReaderAsync())
                while (await reader.ReadAsync()) columns[reader.GetString(0) + "." + reader.GetString(1)] = (reader.GetString(2), reader.GetBoolean(3));
            var compatible = true;
            foreach (var entity in db.Model.GetEntityTypes().Where(e => e.GetTableName()?.StartsWith("AspNet", StringComparison.Ordinal) == true))
            {
                var table = entity.GetTableName()!;
                var store = StoreObjectIdentifier.Table(table, entity.GetSchema());
                foreach (var property in entity.GetProperties())
                {
                    var key = table + "." + property.GetColumnName(store);
                    var expected = property.GetColumnType();
                    if (!columns.TryGetValue(key, out var actual) || !string.Equals(actual.Type, expected, StringComparison.OrdinalIgnoreCase)
                        || actual.Nullable != property.IsNullable)
                    { Console.WriteLine("INCOMPATIBLE: " + key + " (columna, tipo o nulabilidad)."); compatible = false; }
                }
            }
            if (!compatible) { Console.WriteLine("DETENER: esquema Identity incompleto o incompatible. Sin escrituras."); return false; }
            Console.WriteLine("Columnas, tipos y nulabilidad Identity compatibles con modelo EF.");
            command.CommandText = """
                SELECT t.name, i.is_primary_key, i.is_unique, c.name, ic.key_ordinal
                FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
                JOIN sys.indexes i ON i.object_id=t.object_id
                JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
                JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
                WHERE s.name='dbo' AND t.name LIKE 'AspNet%' AND i.is_disabled=0 AND ic.key_ordinal>0
                """;
            var keys = new HashSet<string>();
            await using (var reader = await command.ExecuteReaderAsync())
                while (await reader.ReadAsync())
                {
                    if (reader.GetBoolean(1)) keys.Add("PK:" + reader.GetString(0) + ":" + reader.GetString(3));
                    if (reader.GetBoolean(2)) keys.Add("UNIQUE:" + reader.GetString(0) + ":" + reader.GetString(3));
                }
            foreach (var entity in db.Model.GetEntityTypes().Where(e => e.GetTableName()?.StartsWith("AspNet", StringComparison.Ordinal) == true))
                foreach (var property in entity.FindPrimaryKey()!.Properties)
                    if (!keys.Contains("PK:" + entity.GetTableName() + ":" + property.GetColumnName())) compatible = false;
            compatible &= keys.Contains("UNIQUE:AspNetUsers:NormalizedUserName") && keys.Contains("UNIQUE:AspNetRoles:NormalizedName");
            command.CommandText = """
                SELECT COUNT(*) FROM sys.foreign_keys f JOIN sys.tables t ON t.object_id=f.parent_object_id
                JOIN sys.schemas s ON s.schema_id=t.schema_id
                WHERE s.name='dbo' AND t.name LIKE 'AspNet%' AND f.is_disabled=0 AND f.is_not_trusted=0
                """;
            compatible &= Convert.ToInt32(await command.ExecuteScalarAsync()) >= 6;
            if (!compatible) { Console.WriteLine("DETENER: claves, índices o relaciones Identity requieren revisión. Sin escrituras."); return false; }
            Console.WriteLine("Claves primarias, índices únicos de nombres y relaciones Identity habilitadas verificados.");
            Console.WriteLine("Usuarios: " + await db.Users.CountAsync());
            Console.WriteLine("Roles: " + await db.Roles.CountAsync());
            Console.WriteLine("Asignaciones: " + await db.UserRoles.CountAsync());
            Console.WriteLine("Usuarios MFA: " + await db.Users.CountAsync(u => u.TwoFactorEnabled));
            var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json", true)
                .AddUserSecrets("UGP-ultimo-guerrero-en-pie").AddEnvironmentVariables().Build();
            Console.WriteLine("Reservas habilitadas en configuración Development: " + config.GetValue<bool>("Reservas:Habilitadas"));
            Console.WriteLine("Rol configurado en ESTE proceso de diagnóstico (no en el host HTTPS): " + !string.IsNullOrWhiteSpace(config["Administracion:RolPermitido"]));
            Console.WriteLine("Los overrides de entorno de otro proceso no son visibles en este diagnóstico.");
            if (config.GetValue<bool>("Reservas:Habilitadas")) { Console.WriteLine("DETENER: checkout no está bloqueado en configuración."); return false; }
            Console.WriteLine("Sólo lectura: no se han creado cuentas, roles ni esquema.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("PREFLIGHT BLOQUEADO: " + ex.GetType().Name + "; sin escrituras ni detalles privados.");
            return false;
        }
    }
}
