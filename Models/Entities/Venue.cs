using System.Collections.Generic;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Plantilla de recinto: imagen visual de referencia + zonas/localidades
public partial class Venue
{
    public int VenueId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? SVGData { get; set; } // Legacy: mapa SVG (mantenido para compatibilidad)
    public string? ImagenPrevia { get; set; } // URL imagen de vista previa
    public string? ImagenPlano { get; set; } // URL imagen del plano del recinto
    public string? Descripcion { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;

    public virtual ICollection<VenueZone> Zones { get; set; } = new List<VenueZone>();
    public virtual ICollection<Evento> Eventos { get; set; } = new List<Evento>();
}
