using System;
using System.Collections.Generic;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Representa una transacción de compra de boletos
public partial class Compra
{
    public int CompraId { get; set; }

    public int VisitanteId { get; set; }

    public int EventoId { get; set; }

    public int MetodoPagoId { get; set; }

    public DateTime? FechaCompra { get; set; }

    // Monto total cobrado por la compra
    public decimal MontoTotal { get; set; }

    // Estado del pago (ej. "Pendiente", "Aprobado", "Rechazado", "Cancelado")
    public string? EstadoPago { get; set; }

    public virtual ICollection<DetalleCompra> DetalleCompras { get; set; } = new List<DetalleCompra>();

    public virtual Evento Evento { get; set; } = null!;

    public virtual MetodoPago MetodoPago { get; set; } = null!;

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual Visitante Visitante { get; set; } = null!;
}
