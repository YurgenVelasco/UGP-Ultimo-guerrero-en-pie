using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UGP_ultimo_guerrero_en_pie_.Services.Implementations;

namespace UGP_ultimo_guerrero_en_pie_.Controllers;

[ApiController]
[Route("pagos/wompi/eventos")]
public sealed class WompiEventosController(WompiCheckout wompi, ReservasService reservas, WompiWebhookPolicy policy) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [RequestSizeLimit(32768)]
    public async Task<IActionResult> Recibir([FromBody] JsonElement body, CancellationToken ct)
    {
        if (!Request.IsHttps) return BadRequest();
        if (!policy.Enabled) return StatusCode(503);
        if (!wompi.ValidateEvent(body, out _, out var id)) return Unauthorized();
        // HTTP 200 solo después del commit de la bandeja; el worker reintenta la API.
        var huella = body.GetProperty("signature").GetProperty("checksum").GetString()!.ToUpperInvariant();
        await reservas.RecibirNotificacionAsync(id, ct, huella);
        return Ok();
    }
}
