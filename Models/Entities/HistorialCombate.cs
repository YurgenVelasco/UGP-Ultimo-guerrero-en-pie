using System;
using System.Collections.Generic;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Registro detallado del resultado de un combate completado
public partial class HistorialCombate
{
    public int HistorialId { get; set; }

    public int CombateId { get; set; }

    // Método de victoria (ej. "Sumisión", "KO", "Decisión")
    public string? MetodoVictoria { get; set; }

    // Número total de rondas que duró el combate
    public int? RoundsTotales { get; set; }

    public string? Observaciones { get; set; }

    public virtual Combate Combate { get; set; } = null!;
}
