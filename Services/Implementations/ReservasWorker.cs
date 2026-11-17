using Microsoft.EntityFrameworkCore;
using UGP_ultimo_guerrero_en_pie_.Data;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;

namespace UGP_ultimo_guerrero_en_pie_.Services.Implementations;

public sealed class ReservasWorker(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<ReservasWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var startupScope = scopes.CreateScope();
        var isolatedWebhook = startupScope.ServiceProvider.GetRequiredService<WompiWebhookPolicy>().Enabled;
        if (!configuration.GetValue<bool>("Reservas:Habilitadas") && !isolatedWebhook) return;
        // La latencia del proveedor nunca detiene el reloj de liberación.
        await Task.WhenAll(LoopAsync(true, stoppingToken), LoopAsync(false, stoppingToken));
    }

    private async Task LoopAsync(bool expiracion, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var service = scope.ServiceProvider.GetRequiredService<ReservasService>();
                if (expiracion) await service.ExpirarEventoAsync(ct: ct);
                else
                {
                    var entorno = scope.ServiceProvider.GetRequiredService<WompiCheckout>().EnvironmentName;
                    var ahora = await ReservaTransaccion.AhoraAsync(db, ct);
                    var ids = await db.Set<TransaccionPago>().AsNoTracking()
                        .Where(t => t.Entorno == entorno && t.SiguienteConciliacionUtc <= ahora)
                        .OrderBy(t => t.SiguienteConciliacionUtc).Select(t => t.WompiId).Take(50).ToListAsync(ct);
                    foreach (var id in ids)
                    {
                        try { await service.VerificarAsync(id, ct: ct); }
                        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                        catch (Exception ex)
                        {
                            await service.ReintentarConciliacionAsync(id, ct);
                            logger.LogWarning("Conciliación reprogramada tras {Tipo}.", ex.GetType().Name);
                        }
                    }
                    // Sin ID no existe endpoint de consulta por referencia en este flujo.
                    // Permanece visible para revisión y a la espera de webhook autenticado.
                    await db.Set<IntentoPago>().Where(i => i.Entorno == entorno && i.SiguienteConciliacionUtc <= ahora &&
                            !db.Set<TransaccionPago>().Any(t => t.IntentoPagoId == i.Id))
                        .ExecuteUpdateAsync(s => s.SetProperty(i => i.Estado, "SinIdPorConciliar")
                            .SetProperty(i => i.SiguienteConciliacionUtc, ahora.AddHours(1)), ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError("Procesamiento de reservas pendiente; operación {Operacion}, tipo {Tipo}. Se reintentará sin asumir un rechazo.",
                    expiracion ? "expiracion" : "conciliacion", ex.GetType().Name);
            }
            try { await Task.Delay(TimeSpan.FromSeconds(10), ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        }
    }
}
