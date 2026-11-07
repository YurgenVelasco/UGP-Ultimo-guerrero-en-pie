using System;
using System.Collections.Generic;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Cliente / visitante que puede explorar eventos y realizar compras
public partial class Visitante
{
    public int VisitanteId { get; set; }

    public string Nombre { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string? Telefono { get; set; }

    public DateOnly? FechaNacimiento { get; set; }

    public DateTime? FechaRegistro { get; set; }

    public virtual ICollection<Compra> Compras { get; set; } = new List<Compra>();
}
