using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using UGP_ultimo_guerrero_en_pie_.Data;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;
using UGP_ultimo_guerrero_en_pie_.Services.Interfaces;

namespace UGP_ultimo_guerrero_en_pie_.Services.Implementations;

public class TicketPdfService : ITicketPdfService
{
    private readonly ApplicationDbContext _db;
    private readonly IQrService _qr;

    static TicketPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public TicketPdfService(ApplicationDbContext db, IQrService qr)
    {
        _db = db;
        _qr = qr;
    }

    public async Task<byte[]> GenerarPdfTicketAsync(int ticketId)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Evento)
            .Include(t => t.Asistente)
            .Include(t => t.Compra).ThenInclude(c => c.Visitante)
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null || ticket.Evento == null)
        {
            throw new InvalidOperationException("Boleto o evento no encontrado en el sistema.");
        }

        var asistente = ticket.Asistente ?? new Asistente
        {
            Nombre = ticket.Compra?.Visitante?.Nombre ?? "Asistente Oficial",
            Apellido = "",
            TipoDocumento = "CC",
            NumeroDocumento = "000000000",
            Correo = ticket.Compra?.Visitante?.Correo ?? ""
        };

        byte[] qrBytes = Array.Empty<byte>();
        if (!string.IsNullOrWhiteSpace(ticket.QrPath) && File.Exists(Path.Combine("wwwroot", ticket.QrPath)))
        {
            qrBytes = await File.ReadAllBytesAsync(Path.Combine("wwwroot", ticket.QrPath));
        }
        else if (!string.IsNullOrWhiteSpace(ticket.CodigoQr))
        {
            qrBytes = _qr.GenerarQr(ticket.CodigoQr);
        }

        return GenerarTicket(ticket, ticket.Evento, asistente, qrBytes);
    }

    public async Task<byte[]> GenerarPdfOrdenAsync(int compraId)
    {
        var compra = await _db.Compras
            .Include(c => c.Evento)
            .Include(c => c.Visitante)
            .Include(c => c.Tickets).ThenInclude(t => t.Asistente)
            .FirstOrDefaultAsync(c => c.CompraId == compraId);

        if (compra == null || compra.Evento == null || compra.Visitante == null)
        {
            throw new InvalidOperationException("Compra, evento o comprador no encontrado.");
        }

        return GenerarTicketsCompra(compra.Tickets.ToList(), compra.Evento, compra, compra.Visitante);
    }

    public byte[] GenerarTicket(Ticket ticket, Evento evento, Asistente asistente, byte[] qrImage)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A6);
                page.Margin(10);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("ULTIMATE GRAPPLING PROMOTION")
                        .Bold().FontSize(12).FontColor("#D4AF37");
                    col.Item().AlignCenter().Text("BOLETO OFICIAL DE ACCESO")
                        .Bold().FontSize(8).FontColor(Colors.Grey.Darken3);
                    col.Item().PaddingVertical(4).LineHorizontal(1.5f).LineColor("#D4AF37");
                });

                page.Content().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"FOLIO: {ticket.CodigoUnico}").Bold().FontSize(10).FontColor(Colors.Black);
                            c.Item().Text($"EVENTO: {evento.Nombre}").Bold().FontSize(8);
                            c.Item().Text($"FECHA: {evento.Fecha:dd/MM/yyyy}").FontSize(7.5f);
                            c.Item().Text($"HORA: {evento.Hora:hh:mm tt}").FontSize(7.5f);
                            c.Item().Text($"LUGAR: {evento.Lugar}").FontSize(7.5f);
                            c.Item().Text($"ZONA: {ticket.TipoTicket ?? ticket.Zona ?? "General"}").Bold().FontSize(8).FontColor("#B8860B");
                            c.Item().Text($"PRECIO: ${ticket.Precio:N0}").Bold().FontSize(8);
                        });

                        if (qrImage is { Length: > 0 })
                        {
                            row.ConstantItem(90).Column(qrCol =>
                            {
                                qrCol.Item().AlignRight().Width(80).Height(80).Image(qrImage);
                                qrCol.Item().AlignCenter().Text(ticket.CodigoUnico).FontSize(6).FontColor(Colors.Grey.Darken1);
                            });
                        }
                    });

                    col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    col.Item().Text("DATOS DEL TITULAR DEL BOLETO").Bold().FontSize(7.5f).FontColor(Colors.Black);
                    col.Item().Text($"Nombre: {asistente.Nombre} {asistente.Apellido}").FontSize(7.5f);
                    col.Item().Text($"Documento: {asistente.TipoDocumento} {asistente.NumeroDocumento}").FontSize(7.5f);
                    if (!string.IsNullOrWhiteSpace(asistente.Correo))
                    {
                        col.Item().Text($"Correo: {asistente.Correo}").FontSize(7f);
                    }

                    col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    col.Item().Text("TÉRMINOS DE SEGURIDAD").Bold().FontSize(6.5f);
                    col.Item().Text("Este boleto cuenta con validación criptográfica HMAC-SHA256 única e intransferible. Debe presentarse el código QR en el acceso del evento para su escaneo. Una vez validado, el boleto queda automáticamente inutilizado.").FontSize(5.5f).FontColor(Colors.Grey.Darken2);
                });

                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(1).LineColor("#D4AF37");
                    col.Item().AlignCenter().Text("Ultimate Grappling Promotion • Todos los derechos reservados © 2026")
                        .FontSize(5.5f).FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();
    }

    public byte[] GenerarTicketsCompra(List<Ticket> tickets, Evento evento, Compra compra, Visitante visitante)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("ULTIMATE GRAPPLING PROMOTION")
                        .Bold().FontSize(20).FontColor("#D4AF37");
                    col.Item().AlignCenter().Text("COMPROBANTE Y RESUMEN DE COMPRA")
                        .Bold().FontSize(12).FontColor(Colors.Grey.Darken3);
                    col.Item().PaddingVertical(6).LineHorizontal(2).LineColor("#D4AF37");
                });

                page.Content().Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Orden de Compra: #{compra.CompraId}").Bold().FontSize(13);
                            c.Item().Text($"Comprador: {visitante.Nombre}").FontSize(10);
                            c.Item().Text($"Correo: {visitante.Correo}").FontSize(10);
                            c.Item().Text($"Fecha de Compra: {compra.FechaCompra:dd/MM/yyyy HH:mm}").FontSize(10);
                        });
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Evento: {evento.Nombre}").Bold().FontSize(12);
                            c.Item().Text($"Fecha: {evento.Fecha:dd/MM/yyyy} - {evento.Hora:hh:mm tt}").FontSize(10);
                            c.Item().Text($"Lugar: {evento.Lugar}").FontSize(10);
                            c.Item().Text($"Estado de Pago: {compra.EstadoPago}").Bold().FontSize(10).FontColor(compra.EstadoPago == "Aprobado" ? Colors.Green.Darken2 : Colors.Orange.Darken2);
                        });
                    });

                    col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    col.Item().Text($"BOLETOS EMITIDOS ({tickets.Count})").Bold().FontSize(12).FontColor(Colors.Black);
                    col.Item().PaddingVertical(4);

                    foreach (var t in tickets)
                    {
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                        {
                            c.Item().Row(tr =>
                            {
                                tr.RelativeItem().Column(tc =>
                                {
                                    tc.Item().Text($"Folio: {t.CodigoUnico}").Bold().FontSize(10).FontColor("#B8860B");
                                    tc.Item().Text($"Localidad: {t.TipoTicket ?? t.Zona ?? "General"} | Precio: ${t.Precio:N0}").FontSize(9);
                                    if (t.Asistente != null)
                                    {
                                        tc.Item().Text($"Asistente: {t.Asistente.Nombre} {t.Asistente.Apellido} (Doc: {t.Asistente.NumeroDocumento})").FontSize(9);
                                    }
                                });
                                tr.ConstantItem(100).AlignRight().Text($"Estado: {t.Estado}").Bold().FontSize(9).FontColor(t.Estado == "Activo" ? Colors.Green.Darken2 : Colors.Grey.Darken1);
                            });
                        });
                        col.Item().PaddingVertical(2);
                    }

                    col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    col.Item().AlignRight().Text($"TOTAL PAGADO: ${compra.MontoTotal:N0}").Bold().FontSize(14).FontColor(Colors.Black);

                    col.Item().PaddingVertical(10);
                    col.Item().Text("POLÍTICAS Y ADVERTENCIAS DE ACCESO").Bold().FontSize(8.5f);
                    col.Item().Text("1. Los boletos son personales e intransferibles. Cada boleto incluye un código QR único y seguro que debe presentarse para acceder.\n2. UGP se reserva el derecho de admisión conforme al reglamento de espectáculos deportivos.\n3. Para soporte o aclaraciones, comuníquese con los canales oficiales de Ultimate Grappling Promotion.").FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Ultimate Grappling Promotion © 2026 • ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    x.CurrentPageNumber().FontSize(8);
                });
            });
        }).GeneratePdf();
    }
}
