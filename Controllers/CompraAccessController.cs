using Microsoft.AspNetCore.Mvc;
using UGP_ultimo_guerrero_en_pie_.Services.Implementations;

namespace UGP_ultimo_guerrero_en_pie_.Controllers;

[ApiController]
[Route("api/compras")]
public sealed class CompraAccessController(CompraAccess access) : ControllerBase
{
    [HttpPost("{compraId:int}/acceso")]
    [RequestSizeLimit(4096)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Activar(int compraId, [FromBody] string token) =>
        access.Redeem(HttpContext, compraId, token) ? NoContent() : NotFound();
}
