using System;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Temas permitidos para el asistente IA
public partial class AITema
{
    public int AITemaId { get; set; } // PK
    public string Nombre { get; set; } = ""; // Nombre del tema
    public string? Descripcion { get; set; } // Descripción opcional
    public bool Activo { get; set; } = true; // Visible/oculto
    public int Orden { get; set; } = 0; // Posición en listado
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
