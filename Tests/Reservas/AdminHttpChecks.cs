using System.Net;
using System.Net.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;
using UGP_ultimo_guerrero_en_pie_.Services.Administration;

internal static class AdminHttpChecks
{
    public static async Task Run()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", rsa, System.Security.Cryptography.HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var generated = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
        using var certificate = X509CertificateLoader.LoadPkcs12(generated.Export(X509ContentType.Pfx), null, X509KeyStorageFlags.UserKeySet);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(k => k.Listen(IPAddress.Loopback, 0, o => o.UseHttps(certificate)));
        builder.Configuration.AddInMemoryCollection(new Dictionary<string,string?> { ["Administracion:RolPermitido"] = "FixtureOperator" });
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
        builder.Services.AddIdentityCore<ApplicationUser>(o => { o.Lockout.MaxFailedAccessAttempts = 5; })
            .AddSignInManager().AddDefaultTokenProviders();
        var store = new AdminMemoryStore();
        builder.Services.AddSingleton<IUserStore<ApplicationUser>>(store);
        builder.Services.AddRazorComponents();
        builder.Services.AddCascadingAuthenticationState();
        var settings = new AdminSettings(builder.Configuration, builder.Environment);
        builder.Environment.EnvironmentName = "Production";
        var productionSettings = new AdminSettings(builder.Configuration, builder.Environment);
        builder.Environment.EnvironmentName = "Development";
        builder.Services.AddSingleton(settings);
        builder.Services.AddAuthorization(o => o.AddPolicy(AdminSettings.Policy, p => p.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme)
            .RequireAuthenticatedUser().AddRequirements(new AdminRequirement())));
        builder.Services.AddScoped<IAuthorizationHandler, AdminAuthorizationHandler>();
        builder.Services.AddScoped<AdminOperations>();
        builder.Services.ConfigureApplicationCookie(o =>
        {
            o.Cookie.Name = "__Host-UGP.Identity"; o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Strict; o.SlidingExpiration = false;
            o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = 401; return Task.CompletedTask; };
            o.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = 403; return Task.CompletedTask; };
        });
        builder.Services.AddRateLimiter(o => { o.RejectionStatusCode = 429; o.AddFixedWindowLimiter("admin-login", l => { l.PermitLimit = 20; l.Window = TimeSpan.FromMinutes(1); l.QueueLimit = 0; }); });
        await using var app = builder.Build();
        app.UseAuthentication(); app.UseAuthorization(); app.UseRateLimiter(); app.UseAntiforgery();
        app.MapAdminAccess();
        app.MapGet("/", () => "Página pública fixture");
        app.MapGet("/administracion", async (HttpContext c, AdminOperations operations) => await operations.ReadStatusAsync(c.User)).RequireAuthorization(AdminSettings.Policy);
        // Sólo este ejecutable de prueba: comprueba la guardia ante una modificación ficticia.
        var writes = 0;
        app.MapPost("/fixture/mutation", async (HttpContext c, AdminOperations operations) => { await operations.DemandAsync(c.User); writes++; return Results.Ok(); }).RequireAuthorization(AdminSettings.Policy);
        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        using (var scope = app.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = new ApplicationUser { Id = "fixture-admin", UserName = "fixture-admin", NormalizedUserName = "FIXTURE-ADMIN", SecurityStamp = Guid.NewGuid().ToString(), LockoutEnabled = true };
            admin.PasswordHash = users.PasswordHasher.HashPassword(admin, password);
            store.Users.Add(admin);
            var ordinary = new ApplicationUser { Id = "fixture-ordinary", UserName = "fixture-ordinary", NormalizedUserName = "FIXTURE-ORDINARY", SecurityStamp = Guid.NewGuid().ToString(), LockoutEnabled = true };
            ordinary.PasswordHash = users.PasswordHasher.HashPassword(ordinary, password);
            store.Users.Add(ordinary);
        }
        await app.StartAsync();
        try
        {
            var origin = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            HttpClient Client() => new(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer(),
                ServerCertificateCustomValidationCallback = (_, cert, _, _) => cert?.Thumbprint == certificate.Thumbprint }) { BaseAddress = new Uri(origin) };
            using var anonymous = Client(); using var adminClient = Client(); using var ordinaryClient = Client();
            var checks = 0;
            void Check(bool ok, string name) { if (!ok) throw new Exception("FALLA: " + name); checks++; Console.WriteLine("PASA: " + name); }
            async Task<HttpResponseMessage> Login(HttpClient client, string name, string pass)
            {
                var html = await client.GetStringAsync(settings.LoginPath);
                var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
                Check(match.Success, "formulario SSR emite token CSRF");
                return await client.PostAsync(settings.LoginPath, new FormUrlEncodedContent(new Dictionary<string,string>
                { ["usuario"] = name, ["clave"] = pass, ["__RequestVerificationToken"] = WebUtility.HtmlDecode(match.Groups[1].Value) }));
            }
            Check((await anonymous.GetAsync("/")).StatusCode == HttpStatusCode.OK, "navegación pública sin sesión");
            Check((await anonymous.GetAsync("/administracion")).StatusCode == HttpStatusCode.Unauthorized, "anónimo dashboard 401");
            Check((await anonymous.GetAsync("/api/administracion/estado")).StatusCode == HttpStatusCode.Unauthorized, "API directa anónima 401");
            Check((await anonymous.PostAsync("/fixture/mutation", null)).StatusCode == HttpStatusCode.Unauthorized && writes == 0, "modificación sin autorización no ejecutada");
            Check((await anonymous.PostAsync(settings.LoginPath, new FormUrlEncodedContent(new Dictionary<string,string> { ["usuario"]="x" }))).StatusCode == HttpStatusCode.BadRequest, "login sin CSRF rechazado");
            var login = await Login(adminClient, "fixture-admin", password);
            Check(login.StatusCode == HttpStatusCode.Redirect, "login Identity correcto");
            var cookie = login.Headers.GetValues("Set-Cookie").Single(v => v.StartsWith("__Host-UGP.Identity="));
            Check(cookie.Contains("secure", StringComparison.OrdinalIgnoreCase) && cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase) && cookie.Contains("samesite=strict", StringComparison.OrdinalIgnoreCase), "atributos de cookie seguros");
            Check((await adminClient.GetAsync("/administracion")).StatusCode == HttpStatusCode.OK, "administrador dashboard permitido");
            Check((await adminClient.GetAsync("/api/administracion/estado")).StatusCode == HttpStatusCode.OK, "API protegida con sesión válida");
            var denied = await Login(ordinaryClient, "fixture-ordinary", password);
            Check(denied.StatusCode == HttpStatusCode.OK && (await ordinaryClient.GetAsync("/administracion")).StatusCode == HttpStatusCode.Unauthorized, "usuario sin rol no obtiene sesión administrativa");
            // Crear tickets exclusivamente en memoria para simular cookie caducada y usuario autenticado sin rol.
            async Task SetTicket(HttpClient client, ApplicationUser user, bool expired)
            {
                using var scope = app.Services.CreateScope();
                var signIn = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
                var principal = await signIn.CreateUserPrincipalAsync(user);
                ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim(AdminSettings.ExpiryClaim, DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds().ToString()));
                var options = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>>().Get(IdentityConstants.ApplicationScheme);
                var ticket = new AuthenticationTicket(principal, new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(expired ? -1 : 10) }, IdentityConstants.ApplicationScheme);
                client.DefaultRequestHeaders.Remove("Cookie");
                client.DefaultRequestHeaders.Add("Cookie", "__Host-UGP.Identity=" + options.TicketDataFormat.Protect(ticket));
            }
            using var noRole = Client(); await SetTicket(noRole, store.Users[1], false);
            Check((await noRole.GetAsync("/api/administracion/estado")).StatusCode == HttpStatusCode.Forbidden, "autenticado sin rol API 403");
            Check((await noRole.GetAsync("/administracion")).StatusCode == HttpStatusCode.Forbidden, "autenticado sin rol dashboard 403");
            Check((await noRole.PostAsync("/fixture/mutation", null)).StatusCode == HttpStatusCode.Forbidden && writes == 0, "usuario sin rol no modifica");
            var identity = new ClaimsIdentity(IdentityConstants.ApplicationScheme);
            identity.AddClaim(new Claim(AdminSettings.ExpiryClaim, DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds().ToString()));
            Check(productionSettings.RequireMfa && !productionSettings.SessionValid(new ClaimsPrincipal(identity), DateTimeOffset.UtcNow), "producción exige MFA y rechaza sesión sin segundo factor");
            var emptySettings = new AdminSettings(new ConfigurationBuilder().Build(), builder.Environment);
            Check(string.IsNullOrEmpty(emptySettings.Role), "sin configuración no existe rol administrativo implícito");
            using var expiredClient = Client(); await SetTicket(expiredClient, store.Users[0], true);
            Check((await expiredClient.GetAsync("/administracion")).StatusCode == HttpStatusCode.Unauthorized, "cookie expirada 401");
            store.AllowAdmin = false;
            Check((await adminClient.GetAsync("/api/administracion/estado")).StatusCode == HttpStatusCode.Forbidden, "revocación de rol efectiva en siguiente solicitud");
            store.AllowAdmin = true;
            var logoutHtml = await adminClient.GetStringAsync(settings.LoginPath);
            var csrf = Regex.Match(logoutHtml, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
            Check((await adminClient.PostAsync("/administracion/salir", null)).StatusCode == HttpStatusCode.BadRequest, "logout exige CSRF");
            Check((await adminClient.PostAsync("/administracion/salir", new FormUrlEncodedContent(new Dictionary<string,string>{["__RequestVerificationToken"] = WebUtility.HtmlDecode(csrf)}))).StatusCode == HttpStatusCode.Redirect, "logout correcto");
            Check((await adminClient.GetAsync("/administracion")).StatusCode == HttpStatusCode.Unauthorized, "sesión cerrada no accede");
            for (var i = 0; i < 5; i++) await Login(anonymous, "fixture-admin", "incorrecta");
            Check(store.Users[0].LockoutEnd > DateTimeOffset.UtcNow, "cinco fallos bloquean cuenta fixture");
            HttpResponseMessage limited = await anonymous.GetAsync(settings.LoginPath);
            for (var i = 0; i < 30 && limited.StatusCode != HttpStatusCode.TooManyRequests; i++) limited = await anonymous.GetAsync(settings.LoginPath);
            Check(limited.StatusCode == HttpStatusCode.TooManyRequests, "limitador HTTP devuelve 429");
            Console.WriteLine($"{checks} comprobaciones HTTP/Identity aisladas. SQL no utilizado; usuarios y contraseña efímeros en memoria.");
        }
        finally { await app.StopAsync(); }
    }
}

internal sealed class AdminMemoryStore : IUserPasswordStore<ApplicationUser>, IUserRoleStore<ApplicationUser>, IUserSecurityStampStore<ApplicationUser>, IUserLockoutStore<ApplicationUser>, IUserTwoFactorStore<ApplicationUser>
{
    public List<ApplicationUser> Users { get; } = [];
    public bool AllowAdmin = true;
    public void Dispose() { }
    public Task<string> GetUserIdAsync(ApplicationUser u, CancellationToken c) => Task.FromResult(u.Id);
    public Task<string?> GetUserNameAsync(ApplicationUser u, CancellationToken c) => Task.FromResult(u.UserName);
    public Task SetUserNameAsync(ApplicationUser u,string? n,CancellationToken c) { u.UserName=n; return Task.CompletedTask; }
    public Task<string?> GetNormalizedUserNameAsync(ApplicationUser u,CancellationToken c) => Task.FromResult(u.NormalizedUserName);
    public Task SetNormalizedUserNameAsync(ApplicationUser u,string? n,CancellationToken c) { u.NormalizedUserName=n; return Task.CompletedTask; }
    public Task<IdentityResult> CreateAsync(ApplicationUser u,CancellationToken c) { Users.Add(u); return Task.FromResult(IdentityResult.Success); }
    public Task<IdentityResult> UpdateAsync(ApplicationUser u,CancellationToken c) => Task.FromResult(IdentityResult.Success);
    public Task<IdentityResult> DeleteAsync(ApplicationUser u,CancellationToken c) => throw new NotSupportedException();
    public Task<ApplicationUser?> FindByIdAsync(string id,CancellationToken c) => Task.FromResult(Users.FirstOrDefault(u=>u.Id==id));
    public Task<ApplicationUser?> FindByNameAsync(string n,CancellationToken c) => Task.FromResult(Users.FirstOrDefault(u=>u.NormalizedUserName==n));
    public Task SetPasswordHashAsync(ApplicationUser u,string? h,CancellationToken c) {u.PasswordHash=h;return Task.CompletedTask;}
    public Task<string?> GetPasswordHashAsync(ApplicationUser u,CancellationToken c)=>Task.FromResult(u.PasswordHash);
    public Task<bool> HasPasswordAsync(ApplicationUser u,CancellationToken c)=>Task.FromResult(u.PasswordHash!=null);
    public Task AddToRoleAsync(ApplicationUser u,string r,CancellationToken c)=>throw new NotSupportedException();
    public Task RemoveFromRoleAsync(ApplicationUser u,string r,CancellationToken c)=>throw new NotSupportedException();
    public Task<IList<string>> GetRolesAsync(ApplicationUser u,CancellationToken c)=>Task.FromResult<IList<string>>(u.Id=="fixture-admin" && AllowAdmin ? ["FixtureOperator"] : []);
    public Task<bool> IsInRoleAsync(ApplicationUser u,string r,CancellationToken c)=>Task.FromResult(u.Id=="fixture-admin" && AllowAdmin && r=="FIXTUREOPERATOR");
    public Task<IList<ApplicationUser>> GetUsersInRoleAsync(string r,CancellationToken c)=>Task.FromResult<IList<ApplicationUser>>([]);
    public Task SetSecurityStampAsync(ApplicationUser u,string s,CancellationToken c){u.SecurityStamp=s;return Task.CompletedTask;}
    public Task<string?> GetSecurityStampAsync(ApplicationUser u,CancellationToken c)=>Task.FromResult(u.SecurityStamp);
    public Task<DateTimeOffset?> GetLockoutEndDateAsync(ApplicationUser u,CancellationToken c)=>Task.FromResult(u.LockoutEnd);
    public Task SetLockoutEndDateAsync(ApplicationUser u,DateTimeOffset? d,CancellationToken c){u.LockoutEnd=d;return Task.CompletedTask;}
    public Task<int> IncrementAccessFailedCountAsync(ApplicationUser u,CancellationToken c)=>Task.FromResult(++u.AccessFailedCount);
    public Task ResetAccessFailedCountAsync(ApplicationUser u,CancellationToken c){u.AccessFailedCount=0;return Task.CompletedTask;}
    public Task<int> GetAccessFailedCountAsync(ApplicationUser u,CancellationToken c)=>Task.FromResult(u.AccessFailedCount);
    public Task<bool> GetLockoutEnabledAsync(ApplicationUser u,CancellationToken c)=>Task.FromResult(u.LockoutEnabled);
    public Task SetLockoutEnabledAsync(ApplicationUser u,bool b,CancellationToken c){u.LockoutEnabled=b;return Task.CompletedTask;}
    public Task SetTwoFactorEnabledAsync(ApplicationUser u,bool b,CancellationToken c){u.TwoFactorEnabled=b;return Task.CompletedTask;}
    public Task<bool> GetTwoFactorEnabledAsync(ApplicationUser u,CancellationToken c)=>Task.FromResult(u.TwoFactorEnabled);
}
