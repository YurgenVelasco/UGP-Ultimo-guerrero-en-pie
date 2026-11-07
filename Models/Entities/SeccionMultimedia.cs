using System;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Configuración CMS para la sección de multimedia / galería
public partial class SeccionMultimedia
{
    public int SeccionMultimediaId { get; set; }
    public string Titulo { get; set; } = null!;
    public string? Subtitulo { get; set; }
    public string? Descripcion { get; set; }
    // URL de YouTube para el video destacado principal
    public string? VideoPrincipalUrl { get; set; }
    // URL de YouTube para un video secundario
    public string? VideoSecundarioUrl { get; set; }
    // Lista JSON de elementos de beneficios
    public string? BeneficiosJson { get; set; }
    public string? TextoBotonPrincipal { get; set; }
    public string? UrlBotonPrincipal { get; set; }
    public string? TextoBotonSecundario { get; set; }
    public string? UrlBotonSecundario { get; set; }
    public int Orden { get; set; } = 0;
    public bool Activo { get; set; } = true;
    public DateTime? FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
