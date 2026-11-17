using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using UGP_ultimo_guerrero_en_pie_.Components;
using UGP_ultimo_guerrero_en_pie_.Data;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;
using UGP_ultimo_guerrero_en_pie_.Services.Implementations;
using UGP_ultimo_guerrero_en_pie_.Services.Interfaces;
using UGP_ultimo_guerrero_en_pie_.Services.Administration;
using Microsoft.AspNetCore.Authorization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Cadena de conexión y DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: [51011, 51013]);
        }));

// Autenticación e Identity Core
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddIdentityCookies();
builder.Services.AddCascadingAuthenticationState();
var dataProtection = builder.Services.AddDataProtection();
if (OperatingSystem.IsWindows())
{
    var profile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    if (string.IsNullOrWhiteSpace(profile))
        throw new InvalidOperationException("Se requiere un perfil de Windows para conservar las claves de acceso a compras.");

    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(profile, "ASP.NET", "DataProtection-Keys")))
        .ProtectKeysWithDpapi();
}
builder.Services.AddSingleton<CompraAccess>();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

var adminSettings = new AdminSettings(builder.Configuration, builder.Environment);
builder.Services.AddSingleton(adminSettings);
builder.Services.AddAuthorization(options => options.AddPolicy(AdminSettings.Policy, policy =>
    policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme)
        .RequireAuthenticatedUser().AddRequirements(new AdminRequirement())));
builder.Services.AddScoped<IAuthorizationHandler, AdminAuthorizationHandler>();
builder.Services.AddScoped<AdminOperations>();
builder.Services.AddScoped<AdminCatalogService>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "__Host-UGP.Identity";
    options.Cookie.Path = "/";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.LoginPath = adminSettings.LoginPath;
    options.ExpireTimeSpan = adminSettings.Lifetime;
    options.SlidingExpiration = false;
    // No divulgar la ruta privada mediante redirects desde rutas adivinables.
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    // Límite global conservador: también protege frente a rotación de direcciones IP.
    options.AddFixedWindowLimiter("admin-login", limiter =>
    {
        limiter.PermitLimit = 20;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

// Registro del subsistema de Tickets y Criptoseguridad
builder.Services.AddScoped<IQrService, QrService>();
builder.Services.AddScoped<ITicketPdfService, TicketPdfService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<ICompraService, CompraService>();
builder.Services.AddScoped<WompiCheckout>();
builder.Services.AddScoped<ReservasService>();
builder.Services.AddScoped<WompiWebhookPolicy>();
builder.Services.AddHostedService<ReservasWorker>();
builder.Services.AddHttpClient("Wompi", client => client.Timeout = TimeSpan.FromSeconds(10))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

builder.Services.AddControllers();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();
app.Logger.LogInformation("AdministracionUGP: entorno={Environment}; rol configurado={RoleConfigured}; MFA obligatorio={RequireMfa}; reservas habilitadas={Reservations}; webhook habilitado={Webhook}",
    app.Environment.EnvironmentName, !string.IsNullOrWhiteSpace(adminSettings.Role), adminSettings.RequireMfa,
    builder.Configuration.GetValue<bool>("Reservas:Habilitadas"), builder.Configuration.GetValue<bool>("Wompi:Webhooks:Habilitados"));

// El esquema se administra exclusivamente con scripts SQL revisados.
// El arranque no aplica migraciones ni siembra datos.

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.Use(async (context, next) =>
{
    var administrative = context.Request.Path.StartsWithSegments("/administracion") ||
        context.Request.Path.StartsWithSegments("/api/administracion") || context.Request.Path == adminSettings.LoginPath;
    if (administrative)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
    }
    await next();
    if (administrative && context.Response.StatusCode >= 400)
        context.RequestServices.GetRequiredService<ILogger<AdminOperations>>().LogWarning(
            "AdminAudit RequestDenied Status={Status} Trace={Trace}", context.Response.StatusCode, context.TraceIdentifier);
});
app.UseAuthorization();
app.UseRateLimiter();
app.Use((context, next) => context.RequestServices.GetRequiredService<CompraAccess>().LoadAsync(context, next));
app.UseAntiforgery();

app.MapControllers();
app.MapAdminAccess();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
