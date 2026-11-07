using System;
using System.Collections.Generic;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Categoría de boleto que define precio base y capacidad máxima por tipo
public partial class TipoEntrada
{
    public int TipoEntradaId { get; set; }

    public string Nombre { get; set; } = null!;

    public decimal Precio { get; set; }

    // Número máximo de boletos disponibles para este tipo
    public int Capacidad { get; set; }

    public virtual ICollection<DetalleCompra> DetalleCompras { get; set; } = new List<DetalleCompra>();

    public virtual ICollection<EventoTipoEntrada> EventoTiposEntrada { get; set; } = new List<EventoTipoEntrada>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
