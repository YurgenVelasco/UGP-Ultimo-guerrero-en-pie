using System;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Elemento del carrusel (imagen o video de YouTube) para la sección de experiencia
public partial class ExperienciaMultimediaItem
{
    public int ExperienciaMultimediaItemId { get; set; }
    // Identifica a qué grupo del carrusel pertenece este elemento
    public int CarruselNumero { get; set; }
    // Discriminador: "Imagen" o "VideoYouTube"
    public string Tipo { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string? Titulo { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
}

public static class ExperienciaMultimediaItemTipo
{
    public const string Imagen = "Imagen";
    public const string VideoYouTube = "VideoYouTube";
}
