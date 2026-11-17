using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace UGP_ultimo_guerrero_en_pie_.Services.Implementations;

public sealed class CompraAccess(IDataProtectionProvider protection, IWebHostEnvironment environment)
{
    private const string BrowserCookie = "UGP.Browser";
    private const string OrderPrefix = "UGP.Order.";
    private const string Issuer = "UGP.CompraAccess";
    private const string BrowserClaim = Issuer + ".Browser";
    private const string OrderClaim = Issuer + ".Order";
    private static readonly TimeSpan OrderLifetime = TimeSpan.FromDays(30);
    private readonly IDataProtector _browser = protection.CreateProtector(Issuer, "Browser.v1");
    private readonly ITimeLimitedDataProtector _handoff = protection.CreateProtector(Issuer, "Handoff.v1").ToTimeLimitedDataProtector();
    private readonly ITimeLimitedDataProtector _order = protection.CreateProtector(Issuer, "Order.v1").ToTimeLimitedDataProtector();

    public async Task LoadAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path.StartsWithSegments("/uploads/qr"))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (context.Request.Path.StartsWithSegments("/checkout"))
            context.Response.Headers.CacheControl = "no-store";

        var browser = ReadBrowser(context);
        if (browser is null && HttpMethods.IsGet(context.Request.Method) &&
            context.Request.Path.StartsWithSegments("/checkout") && CanSetCookie(context))
        {
            browser = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            WriteBrowser(context, browser);
        }

        if (browser is not null)
        {
            var identity = new ClaimsIdentity();
            identity.AddClaim(new Claim(BrowserClaim, browser, ClaimValueTypes.String, Issuer));
            foreach (var (name, token) in context.Request.Cookies)
            {
                if (!name.StartsWith(OrderPrefix, StringComparison.Ordinal) ||
                    !int.TryParse(name.AsSpan(OrderPrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var id) ||
                    id <= 0)
                    continue;

                try
                {
                    var payload = _order.Unprotect(token, out var expires);
                    if (payload == Payload(browser, id))
                        identity.AddClaim(new Claim(OrderClaim, FormattableString.Invariant($"{id}:{expires.ToUnixTimeSeconds()}"),
                            ClaimValueTypes.String, Issuer));
                }
                catch (CryptographicException)
                {
                    // Una cookie inválida o vencida no concede acceso.
                }
            }
            context.User.AddIdentity(identity);
        }

        await next(context);
    }

    public bool HasBrowser(ClaimsPrincipal user) => BrowserId(user) is not null;

    public string CreateHandoff(ClaimsPrincipal user, int compraId)
    {
        var browser = BrowserId(user) ?? throw new InvalidOperationException("El navegador no tiene una sesión de compra.");
        return _handoff.Protect(Payload(browser, compraId), TimeSpan.FromMinutes(5));
    }

    public bool Redeem(HttpContext context, int compraId, string token)
    {
        var browser = ReadBrowser(context);
        if (compraId <= 0 || browser is null || string.IsNullOrEmpty(token) || token.Length > 2048 || !CanSetCookie(context))
            return false;

        var payload = Payload(browser, compraId);
        try
        {
            if (_handoff.Unprotect(token) != payload) return false;
        }
        catch (CryptographicException)
        {
            return false;
        }

        WriteBrowser(context, browser);
        context.Response.Cookies.Append(OrderPrefix + compraId.ToString(CultureInfo.InvariantCulture),
            _order.Protect(payload, OrderLifetime), CookieOptions(context, OrderLifetime));
        return true;
    }

    public bool Allows(ClaimsPrincipal user, int compraId) => OrderIds(user).Contains(compraId);

    public IEnumerable<int> OrderIds(ClaimsPrincipal user)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        foreach (var claim in user.FindAll(OrderClaim))
        {
            var separator = claim.Value.IndexOf(':');
            if (claim.Issuer == Issuer && separator > 0 &&
                int.TryParse(claim.Value.AsSpan(0, separator), NumberStyles.None, CultureInfo.InvariantCulture, out var id) &&
                id > 0 && long.TryParse(claim.Value.AsSpan(separator + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var expiry) &&
                expiry > now)
                yield return id;
        }
    }

    private static string? BrowserId(ClaimsPrincipal user) =>
        user.FindAll(BrowserClaim).FirstOrDefault(claim => claim.Issuer == Issuer)?.Value;

    private string? ReadBrowser(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(BrowserCookie, out var token)) return null;
        try { return _browser.Unprotect(token); }
        catch (CryptographicException) { return null; }
    }

    private bool CanSetCookie(HttpContext context) => context.Request.IsHttps ||
        (environment.IsDevelopment() && context.Request.Host.Host is "localhost" or "127.0.0.1" or "[::1]");

    private void WriteBrowser(HttpContext context, string browser) =>
        context.Response.Cookies.Append(BrowserCookie, _browser.Protect(browser), CookieOptions(context, TimeSpan.FromDays(365)));

    private static CookieOptions CookieOptions(HttpContext context, TimeSpan lifetime) => new()
    {
        HttpOnly = true,
        Secure = context.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Path = context.Request.PathBase.HasValue ? context.Request.PathBase.Value : "/",
        MaxAge = lifetime,
        IsEssential = true
    };

    private static string Payload(string browser, int id) => FormattableString.Invariant($"{browser}:{id}");
}
