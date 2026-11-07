using System;
using System.Collections.Generic;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Representa un evento de MMA / grappling con fecha, lugar y capacidad
public partial class Evento
{
    public int EventoId { get; set; }

    public string Nombre { get; set; } = null!;

    public DateOnly Fecha { get; set; }

    public TimeOnly Hora { get; set; }

    public string Lugar { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string? ImagenUrl { get; set; }

    // Capacidad total del recinto
    public int CapacidadMaxima { get; set; }

    // Boletos restantes disponibles para venta
    public int TicketsDisponibles { get; set; }

    // Plantilla de recinto asociada (imagen visual del plano)
    public int? VenueId { get; set; }

    // Estado: "Programado", "Finalizado", "Cancelado", etc.
    public string? EstadoEvento { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public virtual ICollection<Combate> Combates { get; set; } = new List<Combate>();

    public virtual ICollection<Compra> Compras { get; set; } = new List<Compra>();

    public virtual ICollection<EventoTipoEntrada> EventoTiposEntrada { get; set; } = new List<EventoTipoEntrada>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual ICollection<Experiencia> Experiencias { get; set; } = new List<Experiencia>();

    public virtual ICollection<Patrocinio> Patrocinios { get; set; } = new List<Patrocinio>();

    public virtual Venue? Venue { get; set; }
}
