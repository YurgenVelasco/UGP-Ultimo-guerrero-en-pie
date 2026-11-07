using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UGP_ultimo_guerrero_en_pie_.Data;
using UGP_ultimo_guerrero_en_pie_.Models.Enums;
using UGP_ultimo_guerrero_en_pie_.Services.Interfaces;

namespace UGP_ultimo_guerrero_en_pie_.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BoletosController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITicketPdfService _pdfService;

    public BoletosController(ApplicationDbContext db, ITicketPdfService pdfService)
    {
        _db = db;
        _pdfService = pdfService;
    }

    [HttpGet("descargar/{ticketId:int}")]
    public async Task<IActionResult> DescargarTicket(int ticketId)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Compra)
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            return NotFound("El boleto solicitado no existe.");
        }

        if (ticket.Compra?.EstadoPago != EstadoCompra.Aprobado.ToString())
        {
            return BadRequest("El boleto aún no ha sido aprobado o la compra está pendiente.");
        }

        var pdfBytes = await _pdfService.GenerarPdfTicketAsync(ticketId);
        return File(pdfBytes, "application/pdf", $"Boleto-{ticket.CodigoUnico}.pdf");
    }

    [HttpGet("descargar-orden/{compraId:int}")]
    public async Task<IActionResult> DescargarOrden(int compraId)
    {
        var compra = await _db.Compras
            .FirstOrDefaultAsync(c => c.CompraId == compraId);

        if (compra == null)
        {
            return NotFound("La orden de compra solicitada no existe.");
        }

        if (compra.EstadoPago != EstadoCompra.Aprobado.ToString())
        {
            return BadRequest("La orden de compra no se encuentra aprobada.");
        }

        var pdfBytes = await _pdfService.GenerarPdfOrdenAsync(compraId);
        return File(pdfBytes, "application/pdf", $"UGP-Orden-{compraId:D6}.pdf");
    }
}
