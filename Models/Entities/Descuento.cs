using System;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Regla de descuento por volumen aplicada a un tipo de boleto específico
public partial class Descuento
{
    public int DescuentoId { get; set; }

    public int TicketId { get; set; }

    // Cantidad mínima de boletos para calificar al descuento
    public int CantidadMinima { get; set; }

    // Porcentaje de descuento expresado como decimal (ej. 0.15 = 15%)
    public decimal PorcentajeDescuento { get; set; }

    public virtual Ticket Ticket { get; set; } = null!;
}
