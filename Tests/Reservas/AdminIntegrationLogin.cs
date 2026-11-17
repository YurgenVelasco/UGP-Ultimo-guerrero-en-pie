using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UGP_ultimo_guerrero_en_pie_.Data;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Sólo prueba el host loopback; la contraseña permanece en este proceso interactivo.
internal static class AdminIntegrationLogin
{
    public static async Task Run(bool requireMfaChecks = false, bool waitExpiry = false, bool catalogChecks = false)
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected) throw new InvalidOperationException("Se requiere consola local.");
        if (!await AdminIntegrationPreflight.Run()) return;
        Console.Write("Usuario de integración: ");
        var name = Console.ReadLine()?.Trim() ?? "";
        Console.Write("Contraseña (entrada oculta): ");
        var password = AdminIntegrationProvision.ReadHidden();
        var code = "";
        var cookieJar = new CookieContainer();
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = cookieJar })
        { BaseAddress = new Uri("https://localhost:7197"), Timeout = TimeSpan.FromSeconds(20) };
        var evidence = new List<string>();
        void Check(bool ok, string message) { evidence.Add((ok ? "PASA: " : "FALLA: ") + message); Console.WriteLine(evidence[^1]); }
        Check((await client.GetAsync("/administracion")).StatusCode == HttpStatusCode.Unauthorized, "Dashboard anónimo HTTP 401");
        Check((await client.GetAsync("/api/administracion/estado")).StatusCode == HttpStatusCode.Unauthorized, "API anónima HTTP 401");
        var html = await client.GetStringAsync("/acceso-ugp");
        var csrf = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        if (requireMfaChecks)
        {
            foreach (var invalidCode in new[] { "", "invalid" })
            {
                var rejected = await client.PostAsync("/acceso-ugp", new FormUrlEncodedContent(new Dictionary<string,string>
                { ["usuario"] = name, ["clave"] = password, ["codigo"] = invalidCode, ["__RequestVerificationToken"] = WebUtility.HtmlDecode(csrf) }));
                Check(rejected.StatusCode == HttpStatusCode.OK && (await client.GetAsync("/administracion")).StatusCode == HttpStatusCode.Unauthorized,
                    invalidCode.Length == 0 ? "Sin TOTP no se concede sesión" : "TOTP mal formado no concede sesión");
            }
        }
        Console.Write("Código NUEVO de Google Authenticator (entrada oculta; esperar inicio de un intervalo): ");
        code = AdminIntegrationProvision.ReadHidden();
        var login = await client.PostAsync("/acceso-ugp", new FormUrlEncodedContent(new Dictionary<string,string>
        { ["usuario"] = name, ["clave"] = password, ["codigo"] = code, ["__RequestVerificationToken"] = WebUtility.HtmlDecode(csrf) }));
        password = code = "";
        var success = login.StatusCode == HttpStatusCode.Redirect && login.Headers.Location?.OriginalString == "/administracion";
        Check(success, "Login con contraseña del responsable y MFA si corresponde");
        if (success)
        {
            if (catalogChecks) { await AdminCatalogHttps.Run(client, Check); await CheckNoRole(client, name, Check); }
            if (waitExpiry)
            {
                Check((await client.GetAsync("/administracion")).StatusCode == HttpStatusCode.OK, "Sesión nueva MFA: dashboard 200 antes de esperar");
                Check((await client.GetAsync("/api/administracion/estado")).StatusCode == HttpStatusCode.OK, "Sesión nueva MFA: API 200 antes de esperar");
                using var expiredTicketClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }) { BaseAddress = client.BaseAddress };
                expiredTicketClient.DefaultRequestHeaders.Add("Cookie", cookieJar.GetCookieHeader(client.BaseAddress!));
                // No acortar TTL ni alterar reloj, cookie o stamp. No realizar solicitudes durante la espera.
                Console.WriteLine("Esperando 21 minutos reales. Mantén abierta esta consola y el host. No se guardan credenciales ni cookies.");
                for (var minute = 1; minute <= 21; minute++)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1));
                    Console.WriteLine("Espera de expiración: " + minute + "/21 minutos.");
                }
                Check((await client.GetAsync("/administracion")).StatusCode == HttpStatusCode.Unauthorized, "Expiración real: dashboard HTTP 401 después de 21 minutos");
                Check((await client.GetAsync("/api/administracion/estado")).StatusCode == HttpStatusCode.Unauthorized, "Expiración real: API HTTP 401 después de 21 minutos");
                Check((await expiredTicketClient.GetAsync("/administracion")).StatusCode == HttpStatusCode.Unauthorized, "Servidor rechaza ticket vencido reenviado: dashboard 401");
                Check((await expiredTicketClient.GetAsync("/api/administracion/estado")).StatusCode == HttpStatusCode.Unauthorized, "Servidor rechaza ticket vencido reenviado: API 401");
                Directory.CreateDirectory("artifacts/admin621-evidence");
                await File.WriteAllLinesAsync("artifacts/admin621-evidence/expiry-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".txt", evidence);
                return;
            }
            // Copia exclusivamente en memoria para probar revocación de un ticket emitido legítimamente.
            var retainedJar = new CookieContainer();
            foreach (Cookie cookie in cookieJar.GetCookies(client.BaseAddress!)) retainedJar.Add(client.BaseAddress!, new Cookie(cookie.Name, cookie.Value, cookie.Path));
            using var retained = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = retainedJar }) { BaseAddress = client.BaseAddress };
            var dashboard = await client.GetAsync("/administracion");
            Check(dashboard.StatusCode == HttpStatusCode.OK && (await dashboard.Content.ReadAsStringAsync()).Contains("Administración UGP"), "Dashboard Blazor real HTTP 200 y contenido esperado");
            Check((await client.GetAsync("/api/administracion/estado")).StatusCode == HttpStatusCode.OK, "API protegida HTTP 200");
            if (requireMfaChecks) await CheckNoRole(client, name, Check);
            Check(login.Headers.TryGetValues("Set-Cookie", out var cookies) && cookies.Any(c => c.StartsWith("__Host-UGP.Identity=") && c.Contains("secure", StringComparison.OrdinalIgnoreCase) && c.Contains("httponly", StringComparison.OrdinalIgnoreCase)), "Cookie Identity protegida (sin registrar su valor)");
            Check((await client.PostAsync("/administracion/salir", null)).StatusCode == HttpStatusCode.BadRequest, "Logout sin CSRF HTTP 400");
            html = await client.GetStringAsync("/acceso-ugp");
            csrf = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
            var logout = await client.PostAsync("/administracion/salir", new FormUrlEncodedContent(new Dictionary<string,string> { ["__RequestVerificationToken"] = WebUtility.HtmlDecode(csrf) }));
            Check(logout.StatusCode == HttpStatusCode.Redirect, "Cierre de sesión HTTP 302");
            Check((await client.GetAsync("/administracion")).StatusCode == HttpStatusCode.Unauthorized, "Dashboard tras logout HTTP 401");
            if (requireMfaChecks)
            {
                Check((await retained.GetAsync("/api/administracion/estado")).StatusCode == HttpStatusCode.OK, "Ticket emitido aún vigente antes de revocar stamp");
                await Revoke(name);
                var revokedDashboard = (await retained.GetAsync("/administracion")).StatusCode;
                var revokedApi = (await retained.GetAsync("/api/administracion/estado")).StatusCode;
                Check(revokedDashboard is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized, "Revocación SQL: dashboard deniega ticket previo");
                Check(revokedApi is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized, "Revocación SQL: API deniega ticket previo");
            }
        }
        // Sólo resultados fijos: nunca entradas, cookies, semillas, cuerpos ni identificadores personales.
        Directory.CreateDirectory("artifacts/admin621-evidence");
        await File.WriteAllLinesAsync("artifacts/admin621-evidence/login-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".txt", evidence);
        if(catalogChecks) await File.WriteAllLinesAsync("artifacts/admin63-https-results.txt", evidence);
        if(evidence.Any(line=>line.StartsWith("FALLA:",StringComparison.Ordinal))) Environment.ExitCode=2;
    }

    private static async Task Revoke(string name)
    {
        var services = new ServiceCollection().AddLogging();
        services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(AdminIntegrationPreflight.Connection));
        services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByNameAsync(name) ?? throw new InvalidOperationException();
        if (!(await users.GetClaimsAsync(user)).Any(c => c.Type == "ugp.integration.admin621")) throw new InvalidOperationException();
        AdminIntegrationProvision.Require(await users.UpdateSecurityStampAsync(user));
    }

    private static async Task CheckNoRole(HttpClient client, string name, Action<bool,string> check)
    {
        var services = new ServiceCollection().AddLogging();
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(AdminIntegrationPreflight.Connection));
        services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByNameAsync(name) ?? throw new InvalidOperationException("Cuenta ausente.");
        var marker = (await users.GetClaimsAsync(user)).SingleOrDefault(c => c.Type == "ugp.integration.admin621");
        if (marker is null || !await users.IsInRoleAsync(user, marker.Value)) throw new InvalidOperationException("Cuenta no administrada por esta prueba.");
        var csrfHtml=await client.GetStringAsync("/acceso-ugp");
        var roleCsrf=WebUtility.HtmlDecode(Regex.Match(csrfHtml,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        try
        {
            AdminIntegrationProvision.Require(await users.RemoveFromRoleAsync(user, marker.Value));
            check((await client.GetAsync("/administracion")).StatusCode == HttpStatusCode.Forbidden, "Misma sesión autenticada sin rol SQL: dashboard 403");
            check((await client.GetAsync("/api/administracion/estado")).StatusCode == HttpStatusCode.Forbidden, "Misma sesión autenticada sin rol SQL: API 403");
            check((await client.GetAsync("/administracion/eventos")).StatusCode == HttpStatusCode.Forbidden, "Sin rol SQL: eventos 403");
            check((await client.GetAsync("/administracion/localidades")).StatusCode == HttpStatusCode.Forbidden, "Sin rol SQL: localidades 403");
            var deniedPost=await client.PostAsync("/administracion/eventos/nuevo",new FormUrlEncodedContent(new Dictionary<string,string>{{"__RequestVerificationToken",roleCsrf},{"_handler","evento"}}));
            check(deniedPost.StatusCode == HttpStatusCode.Forbidden, "Sin rol SQL y con CSRF válido: modificación HTTP "+(int)deniedPost.StatusCode);
        }
        finally
        {
            if (!await users.IsInRoleAsync(user, marker.Value)) AdminIntegrationProvision.Require(await users.AddToRoleAsync(user, marker.Value));
        }
        check(await users.IsInRoleAsync(user, marker.Value), "Rol de prueba restaurado mediante Identity");
    }
}
