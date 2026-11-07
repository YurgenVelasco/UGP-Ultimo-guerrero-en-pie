using System;
using System.Collections.Generic;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Perfil del peleador / competidor
public partial class Peleador
{
    public int PeleadorId { get; set; }

    public string Nombre { get; set; } = null!;

    // Nombre de ring / apodo
    public string? Apodo { get; set; }

    public string? Nacionalidad { get; set; }

    // Peso en kg
    public decimal? Peso { get; set; }

    public DateOnly? FechaNacimiento { get; set; }

    // Conteo de victorias en su carrera
    public int? PeleasGanadas { get; set; }

    // Conteo de derrotas en su carrera
    public int? PeleasPerdidas { get; set; }

    public string? Descripcion { get; set; }

    public string? FotoUrl { get; set; }

    // Estado: "Activo", "Inactivo", "Retirado"
    public string? Estado { get; set; }

    public DateTime? FechaRegistro { get; set; }

    public virtual ICollection<Combate> CombateGanadors { get; set; } = new List<Combate>();

    public virtual ICollection<Combate> CombatePeleadorAzuls { get; set; } = new List<Combate>();

    public virtual ICollection<Combate> CombatePeleadorRojos { get; set; } = new List<Combate>();
}

// Alias de compatibilidad con esquemas anteriores
public class Peleadore : Peleador
{
}
