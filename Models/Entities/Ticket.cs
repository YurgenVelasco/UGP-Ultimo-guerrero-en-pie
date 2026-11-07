using System;
using System.Collections.Generic;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Boleto individual emitido desde una compra para un evento específico
public partial class Ticket
{
    public int TicketId { get; set; }
    public int CompraId { get; set; }
    public int EventoId { get; set; }
    public int TipoEntradaId { get; set; }

    // Código único legible (formato UGP-2026-000001)
    public string CodigoUnico { get; set; } = null!;

    // Cadena del código QR para validación
    public string CodigoQr { get; set; } = null!;

    // Token hash para validación offline del QR
    public string TokenHash { get; set; } = null!;

    // Categoría del boleto (ej. "General", "VIP", "Preferente")
    public string? TipoTicket { get; set; }

    // Zona / sección de asientos
    public string? Zona { get; set; }

    // Precio pagado por este boleto
    public decimal? Precio { get; set; }

    // Estado actual: "Activo", "Usado", "Cancelado"
    public string? Estado { get; set; }

    public DateTime? FechaGeneracion { get; set; }

    // Referencia al archivo PDF generado (ruta relativa)
    public string? PdfPath { get; set; }

    // Ruta relativa del archivo de imagen QR generado
    public string? QrPath { get; set; }

    public virtual Compra Compra { get; set; } = null!;
    public virtual Evento Evento { get; set; } = null!;
    public virtual TipoEntrada TipoEntrada { get; set; } = null!;
    public virtual Asistente? Asistente { get; set; }

    public virtual ICollection<Descuento> Descuentos { get; set; } = new List<Descuento>();
}
