using System;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Palabras/frases que el asistente no debe responder
public partial class AIPalabraRestringida
{
    public int AIPalabraRestringidaId { get; set; } // PK
    public string Palabra { get; set; } = ""; // Término bloqueado
    public bool Activo { get; set; } = true; // Regla activa/inactiva
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
