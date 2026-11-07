using System;
using System.Collections.Generic;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Representa un combate entre dos peleadores en un evento
public partial class Combate
{
    public int CombateId { get; set; }

    public int EventoId { get; set; }

    // FK al peleador en la esquina roja / primera
    public int PeleadorRojoId { get; set; }

    // FK al peleador en la esquina azul / segunda
    public int PeleadorAzulId { get; set; }

    // FK al peleador ganador (null si empate / sin concurso)
    public int? GanadorId { get; set; }

    // Disciplina (ej. "Grappling", "MMA", "Sumisión")
    public string? TipoCombate { get; set; }

    // Número de rondas programadas o reales
    public int? Rounds { get; set; }

    // Descripción del resultado (ej. "Sumisión", "KO", "Decisión")
    public string? Resultado { get; set; }

    public virtual Evento Evento { get; set; } = null!;

    public virtual Peleador? Ganador { get; set; }

    public virtual ICollection<HistorialCombate> HistorialCombates { get; set; } = new List<HistorialCombate>();

    public virtual Peleador PeleadorAzul { get; set; } = null!;

    public virtual Peleador PeleadorRojo { get; set; } = null!;
}
