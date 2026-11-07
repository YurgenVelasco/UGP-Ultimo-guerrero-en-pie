using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UGP_ultimo_guerrero_en_pie_.Data;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;
using UGP_ultimo_guerrero_en_pie_.Models.Enums;
using UGP_ultimo_guerrero_en_pie_.Models.ViewModels;
using UGP_ultimo_guerrero_en_pie_.Services.Interfaces;

namespace UGP_ultimo_guerrero_en_pie_.Services.Implementations;

public class TicketService : ITicketService
{
    private readonly ApplicationDbContext _db;
    private readonly IQrService _qr;

    public TicketService(ApplicationDbContext db, IQrService qr)
    {
        _db = db;
        _qr = qr;
    }

    public async Task<long> ObtenerSiguienteFolioAsync()
    {
        var connection = _db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT NEXT VALUE FOR seq_ticket_codigo;";

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await _db.Database.OpenConnectionAsync();
        }

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    public async Task<List<Ticket>> GenerarTickets(Compra compra, List<DetalleCompraVm> detalles, List<AsistenteVm> asistentes)
    {
        var fecha = DateTime.UtcNow;
        var tickets = new List<Ticket>();

        foreach (var det in detalles)
        {
            for (int i = 0; i < det.Cantidad; i++)
            {
                var folio = await ObtenerSiguienteFolioAsync();
                var codigoUnico = $"UGP-{fecha.Year}-{folio:D6}";

                string tipoTicket;
                string? zona;
                decimal precio;

                if (det.VenueZoneId.HasValue && det.VenueZoneId > 0)
                {
                    var zone = await _db.VenueZones.FindAsync(det.VenueZoneId.Value);
                    tipoTicket = zone?.Nombre ?? "General";
                    zona = zone?.Nombre;
                    precio = zone?.Precio ?? 0;
                }
                else
                {
                    var tipoEntrada = await _db.TiposEntrada.FindAsync(det.TipoEntradaId);
                    tipoTicket = tipoEntrada?.Nombre ?? "General";
                    zona = tipoEntrada?.Nombre;
                    precio = tipoEntrada?.Precio ?? 0;
                }

                var tokenHash = _qr.GenerarTokenHash(folio, fecha, codigoUnico);
                var contenidoQr = $"UGP|{codigoUnico}|{compra.EventoId}|{folio}|{tokenHash}";

                var qrBytes = _qr.GenerarQr(contenidoQr);
                var qrDir = Path.Combine("wwwroot", "uploads", "qr");
                if (!Directory.Exists(qrDir))
                {
                    Directory.CreateDirectory(qrDir);
                }

                var qrFile = $"qr-{codigoUnico}.png";
                var qrFullPath = Path.Combine(qrDir, qrFile);
                await File.WriteAllBytesAsync(qrFullPath, qrBytes);

                var ticket = new Ticket
                {
                    CompraId = compra.CompraId,
                    EventoId = compra.EventoId,
                    TipoEntradaId = det.TipoEntradaId,
                    CodigoUnico = codigoUnico,
                    CodigoQr = contenidoQr,
                    TokenHash = tokenHash,
                    TipoTicket = tipoTicket,
                    Zona = zona,
                    Precio = precio,
                    Estado = EstadoTicket.Activo.ToString(),
                    FechaGeneracion = fecha,
                    QrPath = $"uploads/qr/{qrFile}"
                };

                tickets.Add(ticket);
            }
        }

        _db.Tickets.AddRange(tickets);
        await _db.SaveChangesAsync();

        // Enlazar asistentes (relación 1:1)
        for (int i = 0; i < tickets.Count && i < asistentes.Count; i++)
        {
            var a = asistentes[i];
            var asistente = new Asistente
            {
                TicketId = tickets[i].TicketId,
                Nombre = a.Nombre,
                Apellido = a.Apellido,
                TipoDocumento = a.TipoDocumento,
                NumeroDocumento = a.Documento,
                Correo = a.Correo,
                Celular = a.Celular,
                FechaNacimiento = DateOnly.TryParse(a.FechaNacimiento, out var fn) ? fn : null
            };
            _db.Asistentes.Add(asistente);
        }

        await _db.SaveChangesAsync();
        return tickets;
    }

    public async Task<TicketVm?> ObtenerTicket(int ticketId)
    {
        return await _db.Tickets.AsNoTracking()
            .Include(t => t.Evento)
            .Include(t => t.Compra).ThenInclude(c => c.Visitante)
            .Where(t => t.TicketId == ticketId)
            .Select(t => new TicketVm
            {
                TicketId = t.TicketId,
                CodigoUnico = t.CodigoUnico,
                CodigoQr = t.CodigoQr,
                TipoTicket = t.TipoTicket,
                Precio = t.Precio,
                Estado = t.Estado,
                EventoNombre = t.Evento.Nombre,
                FechaEvento = t.Evento.Fecha.ToString("ddd dd MMM yyyy"),
                HoraEvento = t.Evento.Hora.ToString("hh:mm tt"),
                Lugar = t.Evento.Lugar,
                CompradorNombre = t.Compra.Visitante.Nombre
            }).FirstOrDefaultAsync();
    }

    public async Task<ValidarTicketVm?> ValidarCodigo(string codigoUnico)
    {
        var t = await _db.Tickets.AsNoTracking()
            .Include(t => t.Evento)
            .Include(t => t.Compra).ThenInclude(c => c.Visitante)
            .Include(t => t.Asistente)
            .FirstOrDefaultAsync(t => t.CodigoUnico == codigoUnico);

        if (t is null) return null;
        return MapearValidacion(t);
    }

    public async Task<ValidarTicketVm?> ValidarQrContenido(string qrContent)
    {
        // Formato: UGP|{CodigoUnico}|{EventoId}|{Folio}|{TokenHash}
        var partes = qrContent.Split('|');
        if (partes.Length < 5 || partes[0] != "UGP")
        {
            return new ValidarTicketVm
            {
                Existe = false,
                Mensaje = "Formato de código QR inválido o no reconocido por UGP."
            };
        }

        var codigoUnico = partes[1];
        if (!long.TryParse(partes[3], out var folio))
        {
            return new ValidarTicketVm
            {
                Existe = false,
                Mensaje = "Identificador de folio corrupto en el código QR."
            };
        }

        var tokenHash = partes[4];

        var ticket = await _db.Tickets.AsNoTracking()
            .Include(t => t.Evento)
            .Include(t => t.Compra).ThenInclude(c => c.Visitante)
            .Include(t => t.Asistente)
            .FirstOrDefaultAsync(t => t.CodigoUnico == codigoUnico);

        if (ticket is null)
        {
            return new ValidarTicketVm
            {
                Existe = false,
                Mensaje = "Boleto no registrado en la base de datos de UGP."
            };
        }

        // Validación criptográfica con HMAC-SHA256
        var fechaGen = ticket.FechaGeneracion ?? DateTime.UtcNow;
        var esFirmaValida = _qr.ValidarTokenHash(folio, fechaGen, codigoUnico, tokenHash);

        if (!esFirmaValida)
        {
            var resInvalido = MapearValidacion(ticket);
            resInvalido.Existe = false;
            resInvalido.Mensaje = "ALERTA DE SEGURIDAD: La firma digital del código QR no coincide o fue manipulada.";
            return resInvalido;
        }

        return MapearValidacion(ticket);
    }

    public async Task<ValidarTicketVm> UsarTicket(int ticketId)
    {
        var t = await _db.Tickets
            .Include(t => t.Evento)
            .Include(t => t.Compra).ThenInclude(c => c.Visitante)
            .Include(t => t.Asistente)
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (t is null)
        {
            return new ValidarTicketVm { Existe = false, Mensaje = "Boleto no encontrado." };
        }

        if (t.Estado != EstadoTicket.Activo.ToString())
        {
            var vm = MapearValidacion(t);
            vm.Mensaje = $"El boleto ya no es válido: Estado actual '{t.Estado}'.";
            return vm;
        }

        t.Estado = EstadoTicket.Usado.ToString();
        await _db.SaveChangesAsync();

        var result = MapearValidacion(t);
        result.Mensaje = "Boleto verificado con éxito. Acceso concedido al evento.";
        return result;
    }

    public async Task<byte[]> RegenerarQr(int ticketId)
    {
        var ticket = await _db.Tickets.FindAsync(ticketId);
        if (ticket is null) return Array.Empty<byte>();

        return _qr.GenerarQr(ticket.CodigoQr);
    }

    private static ValidarTicketVm MapearValidacion(Ticket t)
    {
        var mensaje = t.Estado == EstadoTicket.Activo.ToString()
            ? "Boleto válido y listo para ingreso."
            : t.Estado == EstadoTicket.Usado.ToString()
                ? "Este boleto ya fue utilizado para ingresar."
                : "Boleto cancelado o suspendido.";

        return new ValidarTicketVm
        {
            Existe = true,
            TicketId = t.TicketId,
            CodigoUnico = t.CodigoUnico,
            TipoTicket = t.TipoTicket ?? t.Zona ?? "General",
            Precio = t.Precio ?? 0,
            EstadoTicket = t.Estado ?? EstadoTicket.Activo.ToString(),
            EventoNombre = t.Evento?.Nombre,
            FechaEvento = t.Evento?.Fecha.ToString("ddd dd MMM yyyy"),
            HoraEvento = t.Evento?.Hora.ToString("hh:mm tt"),
            Lugar = t.Evento?.Lugar,
            CompradorNombre = t.Compra?.Visitante?.Nombre,
            AsistenteNombre = t.Asistente != null ? $"{t.Asistente.Nombre} {t.Asistente.Apellido}" : null,
            QrPath = t.QrPath,
            Mensaje = mensaje
        };
    }
}
