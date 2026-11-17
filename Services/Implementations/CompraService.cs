using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UGP_ultimo_guerrero_en_pie_.Data;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;
using UGP_ultimo_guerrero_en_pie_.Models.Enums;
using UGP_ultimo_guerrero_en_pie_.Models.ViewModels;
using UGP_ultimo_guerrero_en_pie_.Services.Interfaces;

namespace UGP_ultimo_guerrero_en_pie_.Services.Implementations;

public class CompraService : ICompraService
{
    private readonly ApplicationDbContext _db;
    private readonly ReservasService _reservas;
    private readonly WompiCheckout _wompi;
    private readonly CompraAccess _access;

    public CompraService(ApplicationDbContext db, ReservasService reservas, WompiCheckout wompi, CompraAccess access)
    {
        _db = db;
        _reservas = reservas;
        _wompi = wompi;
        _access = access;
    }

    public async Task<WompiCheckoutVm> CrearCompraPendienteAsync(CheckoutFormVm modelo)
    {
        ArgumentNullException.ThrowIfNull(modelo);
        modelo.Normalizar();
        foreach (var (_, error) in modelo.Validar())
        {
            throw new ValidationException(error, null, null);
        }


        if (!_wompi.IsConfigured || !_wompi.EventsConfigured || !_wompi.ReservationsEnabled)
        {
            throw new ValidationException("El servicio de pago no está disponible en este momento.");
        }

        var operacionId = Guid.NewGuid(); // Estable durante reintentos de commit ambiguo.
        await _reservas.ExpirarEventoAsync(modelo.EventoId);
        try
        {
            // Ejecución resiliente con estrategia de reintento transitorio de EF Core
            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                _db.ChangeTracker.Clear();
                // Transacción serializable para prevenir sobreventa por concurrencia
                await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                try
                {
                    await ReservaTransaccion.BloquearEventoAsync(_db, modelo.EventoId);
                    var anterior = await _db.Set<ReservaCompra>().AsNoTracking()
                        .Where(r => r.OperacionId == operacionId).Select(r => r.Compra).SingleOrDefaultAsync();
                    if (anterior != null)
                        return new WompiCheckoutVm { CompraId = anterior.CompraId,
                            Referencia = WompiCheckout.Reference(anterior), MontoEnCentavos = WompiCheckout.AmountInCents(anterior), Moneda = _wompi.Currency };
                    var ahora = await ReservaTransaccion.AhoraAsync(_db);
                    var evento = await _db.Eventos.FirstOrDefaultAsync(e => e.EventoId == modelo.EventoId);
                    if (evento is null || evento.EstadoEvento is not ("Activo" or "Próximo"))
                    {
                        throw ErrorCheckout(nameof(modelo.EventoId), "El evento seleccionado ya no está habilitado para venta.");
                    }
                    if (evento.TicketsDisponibles < 0 || evento.TicketsDisponibles > evento.CapacidadMaxima)
                        throw ErrorCheckout(nameof(modelo.EventoId), "El inventario del evento requiere revisión.");

                    var tipoEntrada = await _db.EventoTiposEntrada
                        .Where(ete => ete.EventoId == modelo.EventoId && ete.TipoEntradaId == modelo.TipoEntradaId)
                        .Select(ete => ete.TipoEntrada)
                        .FirstOrDefaultAsync();
                    if (tipoEntrada is null)
                    {
                        throw ErrorCheckout(nameof(modelo.TipoEntradaId), "La localidad no pertenece al evento seleccionado.");
                    }

                    var reservadas = await _db.DetalleCompras
                        .Where(d => d.Compra.EventoId == modelo.EventoId && d.TipoEntradaId == modelo.TipoEntradaId &&
                            (d.Compra.EstadoPago == "Pendiente" || d.Compra.EstadoPago == "Aprobado"))
                        .SumAsync(d => (long)d.Cantidad);
                    var disponibles = Math.Min(evento.TicketsDisponibles, tipoEntrada.Capacidad - reservadas);
                    if (disponibles < modelo.Cantidad)
                    {
                        throw ErrorCheckout(nameof(modelo.Cantidad),
                            $"Quedan {Math.Max(0, disponibles)} boletos disponibles para esa localidad.");
                    }

                    var subtotal = tipoEntrada.Precio * modelo.Cantidad;
                    if (subtotal <= 0 || subtotal > 99_999_999.99m)
                    {
                        throw ErrorCheckout(nameof(modelo.TipoEntradaId), "El precio de la localidad no está disponible para esta cantidad.");
                    }

                    var emailNormalizado = modelo.CorreoComprador;
                    var visitante = await _db.Visitantes.AsNoTracking()
                        .FirstOrDefaultAsync(v => v.Correo.ToLower() == emailNormalizado);

                    if (visitante == null)
                    {
                        visitante = new Visitante
                        {
                            Nombre = modelo.NombreComprador,
                            Correo = emailNormalizado,
                            Telefono = modelo.TelefonoComprador,
                            FechaRegistro = DateTime.UtcNow
                        };
                        _db.Visitantes.Add(visitante);
                        await _db.SaveChangesAsync();
                    }

                    // Método de pago: Wompi
                    var metodoPago = await _db.MetodosPagos
                        .FirstOrDefaultAsync(m => m.Nombre == "Wompi");

                    if (metodoPago == null)
                    {
                        metodoPago = new MetodoPago { Nombre = "Wompi" };
                        _db.MetodosPagos.Add(metodoPago);
                        await _db.SaveChangesAsync();
                    }

                    // Registrar Compra en estado Pendiente
                    var compra = new Compra
                    {
                        EventoId = evento.EventoId,
                        VisitanteId = visitante.VisitanteId,
                        MetodoPagoId = metodoPago.MetodoPagoId,
                        // La columna histórica es datetime: evitar redondear al día siguiente.
                        FechaCompra = new DateTime(ahora.Ticks - ahora.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc),
                        MontoTotal = subtotal,
                        EstadoPago = EstadoCompra.Pendiente.ToString()
                    };

                    _db.Compras.Add(compra);
                    await _db.SaveChangesAsync();

                    var reserva = new ReservaCompra { CompraId = compra.CompraId, CreadaUtc = ahora,
                        ExpiraUtc = ahora.AddMinutes(15), OperacionId = operacionId,
                        AsistentesJson = JsonSerializer.Serialize(modelo.Asistentes) };
                    _db.Add(reserva);
                    _db.Add(new IntentoPago { Id = Guid.NewGuid(), CompraId = compra.CompraId,
                        Referencia = WompiCheckout.Reference(compra), Entorno = _wompi.EnvironmentName,
                        Centavos = WompiCheckout.AmountInCents(compra), Moneda = _wompi.Currency,
                        CreadoUtc = ahora, SiguienteConciliacionUtc = reserva.ExpiraUtc });

                    // Registrar Detalle de la Compra
                    var detalle = new DetalleCompra
                    {
                        CompraId = compra.CompraId,
                        TipoEntradaId = tipoEntrada.TipoEntradaId,
                        Cantidad = modelo.Cantidad,
                        PrecioUnitario = tipoEntrada.Precio,
                        Subtotal = subtotal
                    };

                    _db.DetalleCompras.Add(detalle);

                    // Reducción atómica del aforo disponible
                    evento.TicketsDisponibles -= modelo.Cantidad;

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new WompiCheckoutVm
                    {
                        CompraId = compra.CompraId,
                        Referencia = WompiCheckout.Reference(compra),
                        MontoEnCentavos = WompiCheckout.AmountInCents(compra),
                        Moneda = _wompi.Currency
                    };
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        catch
        {
            // Evita conservar entidades de una reserva revertida al reintentar desde el mismo circuito.
            _db.ChangeTracker.Clear();
            throw;
        }
    }

    private static ValidationException ErrorCheckout(string campo, string mensaje) =>
        new(new ValidationResult(mensaje, [campo]), null, null);


    public Task<WompiCheckoutVm> IniciarCompraAsync(CheckoutFormVm modelo)
    {
        return CrearCompraPendienteAsync(modelo);
    }

    public async Task<string> VerificarPagoWompiAsync(ClaimsPrincipal usuario, int compraId, string transactionId, CancellationToken cancellationToken)
    {
        if (!_access.Allows(usuario, compraId)) return "No se pudo acceder a la orden.";
        if (!_wompi.ReservationsEnabled) return "La verificación está temporalmente en mantenimiento.";
        return await _reservas.VerificarAsync(transactionId, compraId, cancellationToken);
    }

    public async Task<Compra?> ObtenerCompraPorReferenciaAsync(ClaimsPrincipal usuario, string referencia)
    {
        if (string.IsNullOrEmpty(referencia) || referencia.Length is < 22 or > 26)
            return null;

        var partes = referencia.Split('-');
        if (partes.Length != 4 || partes[0] != "UGP" || partes[1] != "TX" ||
            !DateOnly.TryParseExact(partes[2], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
            !int.TryParse(partes[3], NumberStyles.None, CultureInfo.InvariantCulture, out var compraId) ||
            partes[3] != compraId.ToString("D6", CultureInfo.InvariantCulture) || !_access.Allows(usuario, compraId))
            return null;

        var compra = await _db.Compras.AsNoTracking()
            .Include(c => c.Evento).ThenInclude(e => e.Venue)
            .Include(c => c.Visitante)
            .Include(c => c.Tickets).ThenInclude(t => t.Asistente)
            .Include(c => c.DetalleCompras).ThenInclude(d => d.TipoEntrada)
            .FirstOrDefaultAsync(c => c.CompraId == compraId);
        return compra is not null && WompiCheckout.Reference(compra) == referencia ? compra : null;
    }
}
