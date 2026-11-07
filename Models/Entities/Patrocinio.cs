using System;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Acuerdo de patrocinio que vincula a un patrocinador con un evento específico
public partial class Patrocinio
{
    public int PatrocinioId { get; set; }
    public int PatrocinadorId { get; set; }
    // URL opcional de anulación por patrocinio
    public string? UrlPagina { get; set; }
    // Logotipo de anulación por patrocinio (base64 o URL)
    public string? Logo { get; set; }
    // Imagen de volante promocional (base64 o URL)
    public string? Flyer { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    // Tarifa / costo del patrocinio
    public decimal Costo { get; set; }
    public int EventoId { get; set; }
    public string? MensajePromocional { get; set; }
    // Lista JSON de productos / servicios promocionados
    public string? ProductosJson { get; set; }
    public bool Activo { get; set; } = true;
    // Prioridad de orden de visualización (menor = primero)
    public int Orden { get; set; }

    public virtual Evento Evento { get; set; } = null!;
    public virtual Patrocinador Patrocinador { get; set; } = null!;
}
