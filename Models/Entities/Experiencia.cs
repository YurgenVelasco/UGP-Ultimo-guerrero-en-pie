using System;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Experiencia / reseña enviada por el usuario para un evento
public partial class Experiencia
{
    public int ExperienciaId { get; set; }
    public int EventoId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    // Correo de contacto opcional del revisor
    public string? CorreoVisitante { get; set; }
    // Calificación por estrellas (1-5)
    public int? Calificacion { get; set; }
    // Estado de moderación: "Activa", "Rechazada", etc.
    public string Estado { get; set; } = "Activa";
    // URL de YouTube para el video principal
    public string? VideoPrincipalUrl { get; set; }
    // URL de YouTube para un video secundario
    public string? VideoSecundarioUrl { get; set; }
    public DateTime? FechaCreacion { get; set; }

    public virtual Evento Evento { get; set; } = null!;
}
