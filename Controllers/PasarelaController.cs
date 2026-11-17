using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using UGP_ultimo_guerrero_en_pie_.Data;
using UGP_ultimo_guerrero_en_pie_.Models.Enums;
using UGP_ultimo_guerrero_en_pie_.Services.Implementations;

namespace UGP_ultimo_guerrero_en_pie_.Controllers;

[Route("pagos")]
public class PasarelaController(
    ApplicationDbContext context, IConfiguration configuration, IWebHostEnvironment environment,
    WompiCheckout wompi, CompraAccess access, ReservasService reservas) : Controller
{
    [HttpGet("wompi/{compraId:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> RedirigirWompi(int compraId, CancellationToken cancellationToken)
    {
        if (!access.Allows(User, compraId)) return NotFound();

        if (!wompi.IsConfigured || !wompi.ReservationsEnabled || !wompi.EventsConfigured)
        {
            return Problem(statusCode: 503, title: "Wompi no está disponible. Revisa la configuración del entorno de pago.");
        }

        await reservas.ExpirarAsync(compraId, cancellationToken);
        var compra = await context.Compras.AsNoTracking()
            .FirstOrDefaultAsync(c => c.CompraId == compraId && c.MetodoPago.Nombre == "Wompi", cancellationToken);
        if (compra == null)
        {
            return NotFound("Compra no encontrada.");
        }

        var referencia = WompiCheckout.Reference(compra);
        var confirmacion = Url.Content("~/checkout/confirmacion");
        if (compra.EstadoPago != EstadoCompra.Pendiente.ToString())
        {
            return Redirect(QueryHelpers.AddQueryString(confirmacion, "reference", referencia));
        }

        var intento = await context.Set<UGP_ultimo_guerrero_en_pie_.Models.Entities.IntentoPago>()
            .AsNoTracking().Include(i => i.Reserva)
            .SingleOrDefaultAsync(i => i.CompraId == compraId && i.Entorno == wompi.EnvironmentName, cancellationToken);
        var ahora = await ReservaTransaccion.AhoraAsync(context, cancellationToken);
        if (intento == null || intento.Reserva.Estado != "Vigente" || intento.Reserva.ExpiraUtc <= ahora ||
            intento.Referencia != referencia || intento.Centavos != WompiCheckout.AmountInCents(compra) || intento.Moneda != wompi.Currency)
            return Conflict("La reserva no está disponible para pago. Solicita revisión de la orden.");

        var retorno = configuration["Wompi:RedirectUrl"]
            ?? UriHelper.BuildAbsolute(Request.Scheme, Request.Host, Request.PathBase, "/checkout/confirmacion");
        var checkout = configuration["Wompi:CheckoutUrl"] ?? "https://checkout.wompi.co/p/";
        if (!Uri.TryCreate(retorno, UriKind.Absolute, out var retornoUri) ||
            (retornoUri.Scheme != Uri.UriSchemeHttps &&
                !(retornoUri.Scheme == Uri.UriSchemeHttp && retornoUri.IsLoopback)) ||
            !Uri.TryCreate(checkout, UriKind.Absolute, out var checkoutUri) ||
            checkoutUri.AbsoluteUri != "https://checkout.wompi.co/p/")
        {
            return Problem(statusCode: 503, title: "Las URL de Wompi no están configuradas correctamente.");
        }

        var redirectUrl = QueryHelpers.AddQueryString(retorno, "reference", referencia);
        var omitRedirect = environment.IsDevelopment() &&
            (retornoUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || retornoUri.Host == "127.0.0.1");
        var monto = WompiCheckout.AmountInCents(compra);
        if (monto <= 0)
        {
            return BadRequest("El importe de la orden debe ser mayor que cero.");
        }

        var expiracion = WompiCheckout.Expiration(intento.Reserva.ExpiraUtc);
        var firma = wompi.Sign(referencia, monto, expiracion);
        var widgetOptions = new Dictionary<string, object>
        {
            ["currency"] = wompi.Currency,
            ["amountInCents"] = monto,
            ["reference"] = referencia,
            ["publicKey"] = wompi.PublicKey,
            ["expirationTime"] = expiracion,
            ["signature"] = new { integrity = firma }
        };

        var checkoutParameters = new Dictionary<string, string?>
        {
            ["public-key"] = wompi.PublicKey,
            ["currency"] = wompi.Currency,
            ["amount-in-cents"] = FormattableString.Invariant($"{monto}"),
            ["reference"] = referencia,
            ["expiration-time"] = expiracion,
            ["signature:integrity"] = firma
        };
        if (!omitRedirect)
        {
            widgetOptions["redirectUrl"] = redirectUrl;
            checkoutParameters["redirect-url"] = redirectUrl;
        }

        var widget = JsonSerializer.Serialize(widgetOptions);
        var checkoutUrl = QueryHelpers.AddQueryString(checkout, checkoutParameters);

        return Content($$"""
            <!doctype html>
            <html lang="es">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>Pagar con Wompi | UGP</title>
                <link rel="stylesheet" href="{{HtmlEncoder.Default.Encode(Url.Content("~/lib/bootstrap/dist/css/bootstrap.min.css"))}}">
                <script src="https://checkout.wompi.co/widget.js" async></script>
                <script src="{{HtmlEncoder.Default.Encode(Url.Content("~/js/wompi-checkout.js"))}}" defer></script>
            </head>
            <body class="bg-dark text-white">
                <main class="container py-5 text-center">
                    <h1>Pagar con Wompi</h1>
                    <p>Total: <strong>{{compra.MontoTotal:N2}} {{wompi.Currency}}</strong></p>
                    <p>Referencia: {{HtmlEncoder.Default.Encode(referencia)}}</p>
                    <script id="wompi-options" type="application/json">{{widget}}</script>
                    <button id="open-wompi" type="button" class="btn btn-warning" data-return-url="{{HtmlEncoder.Default.Encode(redirectUrl)}}">Abrir pasarela de pago</button>
                    <a class="btn btn-outline-light" href="{{HtmlEncoder.Default.Encode(checkoutUrl)}}">Abrir Web Checkout de Wompi</a>
                    <p id="wompi-status" class="mt-3" role="status" aria-live="polite"></p>
                </main>
            </body>
            </html>
            """, "text/html", Encoding.UTF8);
    }
}
