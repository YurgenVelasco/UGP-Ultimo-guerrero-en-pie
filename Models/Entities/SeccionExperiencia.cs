using System;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Configuración CMS para la sección de inicio "Experiencia" (hero + carrusel)
public partial class SeccionExperiencia
{
    public int SeccionExperienciaId { get; set; }
    public bool Activo { get; set; } = true;
    public string Titulo { get; set; } = null!;
    public string? Subtitulo { get; set; }
    public string? Descripcion { get; set; }
    // Lista JSON de elementos de beneficios (icono + texto)
    public string? BeneficiosJson { get; set; }
    public string? TextoBoton { get; set; }
    public string? UrlBoton { get; set; }
    // Intervalo de reproducción automática en segundos para el carrusel
    public int TiempoAutoplay { get; set; } = 5;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
