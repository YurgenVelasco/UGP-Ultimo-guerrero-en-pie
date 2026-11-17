using Microsoft.EntityFrameworkCore;
using UGP_ultimo_guerrero_en_pie_.Data;

namespace UGP_ultimo_guerrero_en_pie_.Services.Implementations;

internal static class ReservaTransaccion
{
    // Todas las mutaciones de inventario toman el mismo bloqueo por evento,
    // en SQL Server y con propietario Transaction (también entre procesos).
    internal static Task BloquearEventoAsync(ApplicationDbContext db, int eventoId, CancellationToken ct = default)
    {
        return BloquearAsync(db, $"UGP:Inventario:{eventoId}", ct);
    }

    internal static Task BloquearPagoAsync(ApplicationDbContext db, string entorno, string id, CancellationToken ct) =>
        BloquearAsync(db, $"UGP:Wompi:{entorno}:{id}", ct);

    private static Task BloquearAsync(ApplicationDbContext db, string recurso, CancellationToken ct)
    {
        return db.Database.ExecuteSqlInterpolatedAsync($"""
            DECLARE @resultado int;
            EXEC @resultado = sys.sp_getapplock @Resource={recurso},
                @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
            IF @resultado = -1 THROW 51011, 'Tiempo de espera del bloqueo agotado.', 1;
            IF @resultado = -3 THROW 51013, 'Interbloqueo de aplicacion.', 1;
            IF @resultado = -2 THROW 51012, 'Bloqueo cancelado.', 1;
            IF @resultado < 0 THROW 51019, 'Error no transitorio de bloqueo.', 1;
            """, ct);
    }

    internal static Task<DateTime> AhoraAsync(ApplicationDbContext db, CancellationToken ct = default) =>
        db.Database.SqlQuery<DateTime>($"SELECT SYSUTCDATETIME() AS [Value]").SingleAsync(ct);
}
