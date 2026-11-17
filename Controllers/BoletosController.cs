using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UGP_ultimo_guerrero_en_pie_.Data;
using UGP_ultimo_guerrero_en_pie_.Models.Enums;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;
using UGP_ultimo_guerrero_en_pie_.Services.Implementations;
using UGP_ultimo_guerrero_en_pie_.Services.Interfaces;

namespace UGP_ultimo_guerrero_en_pie_.Controllers;

[ApiController]
[Route("api/[controller]")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class BoletosController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITicketPdfService _pdfService;
    private readonly CompraAccess _access;

    public BoletosController(ApplicationDbContext db, ITicketPdfService pdfService, CompraAccess access)
    {
        _db = db;
        _pdfService = pdfService;
        _access = access;
    }

    [HttpGet("descargar/{ticketId:int}")]
    public async Task<IActionResult> DescargarTicket(int ticketId)
    {
        var ticket = await ObtenerBoletoAutorizado(ticketId);
        if (ticket is null) return NotFound();

        var pdfBytes = await _pdfService.GenerarPdfTicketAsync(ticketId);
        return File(pdfBytes, "application/pdf", $"Boleto-{ticket.CodigoUnico}.pdf");
    }

    [HttpGet("descargar-orden/{compraId:int}")]
    public async Task<IActionResult> DescargarOrden(int compraId)
    {
        if (!_access.Allows(User, compraId) ||
            !await _db.Compras.AnyAsync(c => c.CompraId == compraId && c.EstadoPago == EstadoCompra.Aprobado.ToString()))
            return NotFound();

        var pdfBytes = await _pdfService.GenerarPdfOrdenAsync(compraId);
        return File(pdfBytes, "application/pdf", $"UGP-Orden-{compraId:D6}.pdf");
    }

    [HttpGet("qr/{ticketId:int}")]
    public async Task<IActionResult> MostrarQr(int ticketId, [FromServices] IQrService qr)
    {
        var ticket = await ObtenerBoletoAutorizado(ticketId);
        if (ticket is null || string.IsNullOrWhiteSpace(ticket.CodigoQr)) return NotFound();

        return File(qr.GenerarQr(ticket.CodigoQr), "image/png");
    }

    private async Task<Ticket?> ObtenerBoletoAutorizado(int ticketId)
    {
        var ordenes = _access.OrderIds(User).ToArray();
        if (ticketId <= 0 || ordenes.Length == 0) return null;

        return await _db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.TicketId == ticketId &&
            ordenes.Contains(t.CompraId) &&
            t.Compra.EstadoPago == EstadoCompra.Aprobado.ToString() &&
            (t.Estado == EstadoTicket.Activo.ToString() || t.Estado == EstadoTicket.Usado.ToString()));
    }
}
