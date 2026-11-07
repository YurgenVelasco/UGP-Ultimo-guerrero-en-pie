using Microsoft.AspNetCore.Identity;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

public class ApplicationUser : IdentityUser
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string? DocumentoIdentidad { get; set; }
    public string? Telefono { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}
