namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Zona/localidad dentro de un recinto
public partial class VenueZone
{
    public int VenueZoneId { get; set; }
    public int VenueId { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal Precio { get; set; }
    public int Capacidad { get; set; }
    public int Disponibles { get; set; }
    public string Color { get; set; } = null!;
    public int Orden { get; set; }
    public string? SVGPathId { get; set; } // Legacy: ID SVG
    public bool Activo { get; set; }
    public bool TieneAsientosNumerados { get; set; }

    public virtual Venue Venue { get; set; } = null!;
}
