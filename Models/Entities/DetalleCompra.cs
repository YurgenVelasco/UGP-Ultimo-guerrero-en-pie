using System;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Partida dentro de una compra — tipo de boleto + cantidad
public partial class DetalleCompra
{
    public int DetalleId { get; set; }

    public int CompraId { get; set; }

    public int TipoEntradaId { get; set; }

    public int Cantidad { get; set; }

    // Precio unitario al momento de la compra (instantánea)
    public decimal PrecioUnitario { get; set; }

    // Cantidad * PrecioUnitario
    public decimal Subtotal { get; set; }

    public virtual Compra Compra { get; set; } = null!;

    public virtual TipoEntrada TipoEntrada { get; set; } = null!;
}
