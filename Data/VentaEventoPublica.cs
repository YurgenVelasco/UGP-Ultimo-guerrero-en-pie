using Microsoft.EntityFrameworkCore;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;

namespace UGP_ultimo_guerrero_en_pie_.Data;

internal sealed class LocalidadPublica
{
    public int EventoId { get; init; }
    public int TipoEntradaId { get; init; }
    public required string Nombre { get; init; }
    public decimal Precio { get; init; }
    public long Cupos { get; init; }
}

internal static class VentaEventoPublica
{
    internal static bool Habilitada(Evento evento) =>
        evento.TicketsDisponibles > 0 && evento.EstadoEvento is ("Activo" or "Próximo");

    internal static IQueryable<LocalidadPublica> LocalidadesDisponibles(ApplicationDbContext db)
    {
        return db.EventoTiposEntrada.AsNoTracking()
            .Where(t => t.TipoEntrada.Precio > 0 && t.TipoEntrada.Precio <= 99_999_999.99m)
            .Select(t => new
            {
                t.EventoId,
                t.TipoEntradaId,
                t.TipoEntrada.Nombre,
                t.TipoEntrada.Precio,
                Cupos = t.TipoEntrada.Capacidad - db.DetalleCompras
                    .Where(d => d.Compra.EventoId == t.EventoId && d.TipoEntradaId == t.TipoEntradaId &&
                        (d.Compra.EstadoPago == "Pendiente" || d.Compra.EstadoPago == "Aprobado"))
                    .Sum(d => (long?)d.Cantidad).GetValueOrDefault()
            })
            .Where(t => t.Cupos > 0)
            .Select(t => new LocalidadPublica
            {
                EventoId = t.EventoId,
                TipoEntradaId = t.TipoEntradaId,
                Nombre = t.Nombre,
                Precio = t.Precio,
                Cupos = t.Cupos
            });
    }
}
