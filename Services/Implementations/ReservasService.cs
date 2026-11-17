using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UGP_ultimo_guerrero_en_pie_.Data;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;
using UGP_ultimo_guerrero_en_pie_.Models.ViewModels;
using UGP_ultimo_guerrero_en_pie_.Services.Interfaces;

namespace UGP_ultimo_guerrero_en_pie_.Services.Implementations;

public sealed class ReservasService(ApplicationDbContext db, WompiCheckout wompi, ITicketService tickets, ILogger<ReservasService> logger,
    WompiWebhookPolicy? webhookPolicy = null)
{
    public async Task ExpirarEventoAsync(int? eventoId = null, CancellationToken ct = default)
    {
        if (!wompi.ReservationsEnabled && webhookPolicy?.Enabled != true) return;
        var ahora = await ReservaTransaccion.AhoraAsync(db, ct);
        var ids = await db.Set<ReservaCompra>().AsNoTracking()
            .Where(r => r.Estado == "Vigente" && r.ExpiraUtc <= ahora && (r.ReintentarLiberacionUtc == null || r.ReintentarLiberacionUtc <= ahora) &&
                (eventoId == null || r.Compra.EventoId == eventoId))
            .OrderBy(r => r.ExpiraUtc).Select(r => r.CompraId).Take(100).ToListAsync(ct);
        foreach (var id in ids)
        {
            try { await ExpirarAsync(id, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                var reintentar = (await ReservaTransaccion.AhoraAsync(db, ct)).AddMinutes(1);
                await db.Set<ReservaCompra>().Where(r => r.CompraId == id && r.Estado == "Vigente")
                    .ExecuteUpdateAsync(u => u.SetProperty(r => r.ReintentarLiberacionUtc, reintentar), ct);
                logger.LogError("Reserva {CompraId} pendiente de liberación: {Tipo}. No se modificó el inventario de esa operación.", id, ex.GetType().Name);
            }
        }
    }

    public async Task ExpirarAsync(int compraId, CancellationToken ct = default)
    {
        await EnCompraAsync(compraId, async (reserva, ahora) =>
        {
            if (reserva.Estado != "Vigente" || reserva.ExpiraUtc > ahora) return;
            await LiberarAsync(reserva, ahora, "Expirada", ct);
            var intento = await db.Set<IntentoPago>().SingleAsync(i => i.CompraId == compraId, ct);
            intento.Estado = "PorConciliar";
            intento.SiguienteConciliacionUtc = ahora;
        }, ct);
    }

    // Huella solo después de autenticar; null asegura la fila desde una consulta privada.
    public async Task RecibirNotificacionAsync(string id, CancellationToken ct, string? huella = null)
    {
        if (!WompiCheckout.ValidTransactionId(id)) throw new ArgumentException("ID inválido.");
        if (huella != null && (huella.Length != 64 || !huella.All(Uri.IsHexDigit)))
            throw new ArgumentException("Huella inválida.");
        huella = huella?.ToUpperInvariant();
        await EnTransaccionPagoAsync(id, async ahora =>
        {
            var t = await db.Set<TransaccionPago>().SingleOrDefaultAsync(t => t.Entorno == wompi.EnvironmentName && t.WompiId == id, ct);
            if (t == null)
            {
                t = new TransaccionPago { Id = Guid.NewGuid(), Entorno = wompi.EnvironmentName,
                    WompiId = id, RegistradaUtc = ahora, SiguienteConciliacionUtc = ahora };
                db.Add(t);
            }
            if (huella != null && !await db.Set<NotificacionPago>().AnyAsync(n => n.TransaccionPagoId == t.Id && n.Huella == huella, ct))
            {
                db.Add(new NotificacionPago { Id = Guid.NewGuid(), TransaccionPagoId = t.Id, Huella = huella, RecibidaUtc = ahora });
                ConciliacionPolicy.Notificar(t, ahora, duplicada: false);
            }
        }, ct);
    }

    private async Task EnTransaccionPagoAsync(string id, Func<DateTime, Task> action, CancellationToken ct)
    {
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await ReservaTransaccion.BloquearPagoAsync(db, wompi.EnvironmentName, id, ct);
            await action(await ReservaTransaccion.AhoraAsync(db, ct));
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }

    private async Task RegistrarResultadoAsync(string id, long version, Guid operacion, string resultado, TimeSpan demora, CancellationToken ct)
    {
        await EnTransaccionPagoAsync(id, async ahora =>
        {
            var t = await db.Set<TransaccionPago>().SingleOrDefaultAsync(t => t.WompiId == id && t.Entorno == wompi.EnvironmentName, ct);
            if (t == null) return;
            await RegistrarVerificacionAsync(t, version, operacion, ahora, null, resultado, ct);
            if (ConciliacionPolicy.EsObsoleta(t, version)) return;
            t.UltimoResultado = resultado;
            ConciliacionPolicy.Completar(t, version, ahora, ahora.Add(demora));
        }, ct);
    }

    private async Task RegistrarVerificacionAsync(TransaccionPago t, long version, Guid operacion, DateTime ahora,
        string? estado, string resultado, CancellationToken ct)
    {
        if (!await db.Set<VerificacionPago>().AnyAsync(v => v.Id == operacion, ct))
            db.Add(new VerificacionPago { Id = operacion, TransaccionPagoId = t.Id, VersionConsultada = version,
                ConsultadaUtc = ahora, EstadoObservado = estado, Resultado = resultado });
    }

    public async Task ReintentarConciliacionAsync(string id, CancellationToken ct)
    {
        var operacion = Guid.NewGuid();
        await EnTransaccionPagoAsync(id, async ahora =>
        {
            var t = await db.Set<TransaccionPago>().SingleAsync(t => t.WompiId == id && t.Entorno == wompi.EnvironmentName, ct);
            t.UltimoResultado = "FalloProcesamiento";
            t.SiguienteConciliacionUtc = ahora.AddMinutes(1);
            await RegistrarVerificacionAsync(t, t.VersionProcesada, operacion, ahora, null, "FalloProcesamiento", ct);
        }, ct);
    }

    public async Task<string> VerificarAsync(string id, int? compraAutorizada = null, CancellationToken ct = default)
    {
        if ((!wompi.ReservationsEnabled && webhookPolicy?.Enabled != true) || !WompiCheckout.ValidTransactionId(id)) return "No se pudo verificar el pago.";
        var version = await db.Set<TransaccionPago>().AsNoTracking()
            .Where(t => t.Entorno == wompi.EnvironmentName && t.WompiId == id)
            .Select(t => (long?)t.VersionNotificada).SingleOrDefaultAsync(ct) ?? 0;
        var operacion = Guid.NewGuid();
        PagoWompi? pago;
        try { pago = await wompi.GetPaymentAsync(id, ct); }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or IOException ||
            ex is OperationCanceledException && !ct.IsCancellationRequested) { pago = null; }
        if (pago == null)
        {
            // No reemplazar nunca un estado verificado por un fallo de transporte.
            await RegistrarResultadoAsync(id, version, operacion, "SinRespuestaVerificable", TimeSpan.FromMinutes(1), ct);
            return "Wompi no respondió con un pago verificable. La conciliación continuará.";
        }
        var intento = await db.Set<IntentoPago>().AsNoTracking().SingleOrDefaultAsync(i =>
            i.Entorno == wompi.EnvironmentName && i.Referencia == pago.Referencia, ct);
        if (intento == null || (compraAutorizada.HasValue && intento.CompraId != compraAutorizada) ||
            intento.Centavos != pago.Centavos || intento.Moneda != pago.Moneda)
        {
            // Mantener la bandeja para revisión de referencias históricas/desconocidas.
            if (!compraAutorizada.HasValue)
                await RegistrarResultadoAsync(id, version, operacion, "RequiereRevisionVinculo", TimeSpan.FromHours(1), ct);
            return "La transacción no coincide con una reserva de esta compra.";
        }

        // El navegador no puede llenar la bandeja con IDs arbitrarios: primero API + orden.
        await RecibirNotificacionAsync(id, ct);
        var resultado = "Pago registrado para conciliación.";
        await EnCompraAsync(intento.CompraId, async (reserva, ahora) =>
        {
            var i = await db.Set<IntentoPago>().SingleAsync(x => x.Id == intento.Id, ct);
            var t = await db.Set<TransaccionPago>().SingleAsync(x => x.Entorno == wompi.EnvironmentName && x.WompiId == id, ct);
            if (t.IntentoPagoId.HasValue && t.IntentoPagoId != i.Id) throw new InvalidOperationException("Vínculo de pago inconsistente.");
            if (i.Centavos != WompiCheckout.AmountInCents(reserva.Compra) || i.Referencia != WompiCheckout.Reference(reserva.Compra))
                throw new InvalidOperationException("La compra difiere del intento persistido.");
            if (ConciliacionPolicy.EsObsoleta(t, version))
            {
                await RegistrarVerificacionAsync(t, version, operacion, ahora, pago.Estado, "RespuestaObsoleta", ct);
                return;
            }
            await RegistrarVerificacionAsync(t, version, operacion, ahora, pago.Estado, "APIValidada", ct);
            t.IntentoPagoId = i.Id;
            t.VerificadaUtc = ahora;
            t.UltimoResultado = "APIValidada";
            i.UltimaVerificacionUtc = ahora;
            // APPROVED es absorbente. Una lectura PENDING atrasada no revierte un terminal.
            if (ConciliacionPolicy.ConservarEstado(t.Estado, pago.Estado))
            {
                ConciliacionPolicy.Completar(t, version, ahora, null);
                resultado = reserva.Estado == "Confirmada" ? "El pago ya fue verificado." : "El pago está en conciliación.";
                return;
            }
            t.Estado = pago.Estado;
            ConciliacionPolicy.Completar(t, version, ahora, pago.Estado == "PENDING" ? ahora.AddMinutes(1) : null);
            if (reserva.Estado == "Vigente" && reserva.ExpiraUtc <= ahora)
                await LiberarAsync(reserva, ahora, "Expirada", ct);

            if (pago.Estado == "APPROVED")
            {
                if (reserva.Estado == "Vigente" && reserva.Compra.EstadoPago == "Pendiente")
                {
                    reserva.Estado = "Confirmada";
                    reserva.ConfirmadaUtc = ahora;
                    reserva.Compra.EstadoPago = "Aprobado";
                    await EmitirAsync(reserva, ct);
                    i.Estado = "Aprobado";
                    i.SiguienteConciliacionUtc = null;
                    resultado = "Pago confirmado por Wompi.";
                }
                else
                {
                    if (!await db.Set<IncidenciaPago>().AnyAsync(x => x.TransaccionPagoId == t.Id, ct))
                        db.Add(new IncidenciaPago { Id = Guid.NewGuid(), TransaccionPagoId = t.Id, CreadaUtc = ahora,
                            Motivo = reserva.Estado == "Confirmada" ? "CobroAdicional" : "AprobadoSinReserva" });
                    i.Estado = "PorConciliar";
                    resultado = "Pago recibido para revisión del comercio. No se emitieron boletos adicionales.";
                }
            }
            else if (pago.Estado is "DECLINED" or "VOIDED" or "ERROR")
            {
                // Cerramos esta compra; una aprobación de otro intento posterior se concilia.
                await LiberarAsync(reserva, ahora, pago.Estado == "VOIDED" ? "Cancelada" : "Rechazada", ct);
                if (reserva.Estado != "Confirmada") i.Estado = "PorConciliar";
                resultado = "Wompi no aprobó la transacción. No se emitieron boletos.";
            }
            else
            {
                i.SiguienteConciliacionUtc = ahora.AddMinutes(1);
                resultado = "El pago sigue pendiente en Wompi; no se emitieron boletos.";
            }
        }, ct, id);
        return resultado;
    }

    private async Task EnCompraAsync(int compraId, Func<ReservaCompra, DateTime, Task> action, CancellationToken ct, string? wompiId = null)
    {
        var eventoId = await db.Compras.AsNoTracking().Where(c => c.CompraId == compraId).Select(c => (int?)c.EventoId).SingleOrDefaultAsync(ct);
        if (!eventoId.HasValue) return;
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await ReservaTransaccion.BloquearEventoAsync(db, eventoId.Value, ct);
            if (wompiId != null) await ReservaTransaccion.BloquearPagoAsync(db, wompi.EnvironmentName, wompiId, ct);
            var reserva = await db.Set<ReservaCompra>().Include(r => r.Compra).SingleOrDefaultAsync(r => r.CompraId == compraId, ct);
            if (reserva == null) return; // Históricas: nunca liberar/emitir sin adopción revisada.
            await action(reserva, await ReservaTransaccion.AhoraAsync(db, ct));
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }

    private async Task LiberarAsync(ReservaCompra r, DateTime ahora, string motivo, CancellationToken ct)
    {
        if (r.Estado != "Vigente" || r.Compra.EstadoPago == "Aprobado") return;
        if (await db.Tickets.AnyAsync(t => t.CompraId == r.CompraId, ct))
            throw new InvalidOperationException("Reserva no confirmada con boletos existentes: requiere revisión.");
        var cantidad = await db.DetalleCompras.Where(d => d.CompraId == r.CompraId).SumAsync(d => (long)d.Cantidad, ct);
        if (cantidad <= 0 || cantidad > int.MaxValue) throw new InvalidOperationException("Detalle de reserva inconsistente.");
        var cupos = (int)cantidad;
        var actualizadas = await db.Eventos.Where(e => e.EventoId == r.Compra.EventoId && e.TicketsDisponibles >= 0 &&
                e.CapacidadMaxima - e.TicketsDisponibles >= cupos)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.TicketsDisponibles, e => e.TicketsDisponibles + cupos), ct);
        if (actualizadas != 1) throw new InvalidOperationException("Inventario inconsistente: liberación revertida.");
        r.Estado = "Liberada";
        r.LiberadaUtc = ahora;
        r.MotivoLiberacion = motivo;
        r.AsistentesJson = null;
        r.Compra.EstadoPago = motivo switch { "Rechazada" => "Rechazado", "Cancelada" => "Cancelado", _ => "Expirada" };
    }

    private async Task EmitirAsync(ReservaCompra r, CancellationToken ct)
    {
        if (await db.Tickets.AnyAsync(t => t.CompraId == r.CompraId, ct))
            throw new InvalidOperationException("Reserva pendiente con tickets existentes.");
        var detalles = await db.DetalleCompras.Where(d => d.CompraId == r.CompraId)
            .Select(d => new DetalleCompraVm { TipoEntradaId = d.TipoEntradaId, Cantidad = d.Cantidad }).ToListAsync(ct);
        var inputs = JsonSerializer.Deserialize<List<AsistenteInputVm>>(r.AsistentesJson ?? "[]") ?? [];
        if (inputs.Count != detalles.Sum(d => d.Cantidad)) throw new InvalidOperationException("Faltan asistentes persistidos.");
        var asistentes = inputs.Select(a =>
        {
            var nombre = a.NombreCompleto.Trim().Split(' ', 2);
            return new AsistenteVm { Nombre = nombre[0], Apellido = nombre.Length > 1 ? nombre[1] : "",
                TipoDocumento = a.TipoDocumento, Documento = a.DocumentoIdentidad,
                Correo = a.CorreoElectronico, Celular = a.Telefono ?? "" };
        }).ToList();
        await tickets.GenerarTickets(r.Compra, detalles, asistentes);
        r.AsistentesJson = null;
    }
}
