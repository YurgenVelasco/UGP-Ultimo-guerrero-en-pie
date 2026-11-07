using System;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Secciones de contexto que se envían al modelo (eventos, entradas, etc.)
public partial class AIContextoConfig
{
    public int AIContextoConfigId { get; set; } // PK
    public string Clave { get; set; } = ""; // Identificador único (ej. "eventos")
    public string Nombre { get; set; } = ""; // Nombre visible
    public string? Descripcion { get; set; } // Descripción del contexto
    public bool Activo { get; set; } = true; // Incluir en el prompt
    public int Orden { get; set; } = 0; // Orden en que se envía
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
