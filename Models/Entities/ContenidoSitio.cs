using System;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Bloque de contenido CMS clave-valor para secciones dinámicas del sitio
public partial class ContenidoSitio
{
    public int ContenidoSitioId { get; set; }

    // Identificador único de contenido (slug) usado para buscar el bloque
    public string Clave { get; set; } = null!;

    public string? Titulo { get; set; }

    public string? Subtitulo { get; set; }

    public string? Descripcion { get; set; }

    // Ruta / URL a una imagen asociada
    public string? RutaImagen { get; set; }

    // Metadatos JSON flexibles para campos de contenido adicionales
    public string? MetadatosJson { get; set; }

    public DateTime FechaActualizacion { get; set; }
}
