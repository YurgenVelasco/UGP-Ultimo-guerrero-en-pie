using System;
using System.Collections.Generic;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Entidad de empresa patrocinadora
public partial class Patrocinador
{
    public int PatrocinadorId { get; set; }

    public string Empresa { get; set; } = null!;

    // Nombre de la persona de contacto en la empresa patrocinadora
    public string? Contacto { get; set; }

    public string? Correo { get; set; }

    public string? Telefono { get; set; }

    public string? Descripcion { get; set; }

    // URL del sitio web externo del patrocinador
    public string? UrlPagina { get; set; }

    // URL del logotipo de la marca del patrocinador
    public string? LogoUrl { get; set; }

    public DateTime? FechaRegistro { get; set; }

    public virtual ICollection<Patrocinio> Patrocinios { get; set; } = new List<Patrocinio>();
}

// Alias de compatibilidad con esquemas anteriores
public class Patrocinadore : Patrocinador
{
}
