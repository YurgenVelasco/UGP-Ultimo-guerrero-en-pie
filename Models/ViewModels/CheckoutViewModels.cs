using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UGP_ultimo_guerrero_en_pie_.Models.ViewModels;

public class AsistenteInputVm
{
    [Required(ErrorMessage = "El nombre completo del asistente es requerido")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de documento es requerido")]
    public string TipoDocumento { get; set; } = "CC";

    [Required(ErrorMessage = "El número de documento de identidad es requerido")]
    [StringLength(30, MinimumLength = 4, ErrorMessage = "Documento no válido")]
    public string DocumentoIdentidad { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es requerido")]
    [EmailAddress(ErrorMessage = "Formato de correo electrónico no válido")]
    public string CorreoElectronico { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Número de teléfono no válido")]
    public string? Telefono { get; set; }
}

public class CheckoutFormVm
{
    [Required]
    public int EventoId { get; set; }

    [Required]
    public int TipoEntradaId { get; set; }

    [Range(1, 10, ErrorMessage = "La cantidad permitida es entre 1 y 10 boletos por compra")]
    public int Cantidad { get; set; } = 1;

    // Datos del comprador principal
    [Required(ErrorMessage = "El nombre del comprador es requerido")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres")]
    public string NombreComprador { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo del comprador es requerido")]
    [EmailAddress(ErrorMessage = "Formato de correo no válido")]
    public string CorreoComprador { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de documento es requerido")]
    public string TipoDocumento { get; set; } = "CC";

    [Required(ErrorMessage = "El documento de identidad es requerido")]
    [StringLength(30, MinimumLength = 4, ErrorMessage = "Documento no válido")]
    public string DocumentoComprador { get; set; } = string.Empty;

    public string? TelefonoComprador { get; set; }

    // Lista de asistentes para cada boleto individual
    public List<AsistenteInputVm> Asistentes { get; set; } = new();

    public decimal MontoTotal { get; set; }
}

public class WompiCheckoutVm
{
    public int CompraId { get; set; }
    public string Referencia { get; set; } = string.Empty;
    public long MontoEnCentavos { get; set; }
    public string Moneda { get; set; } = "COP";
    public string PublicKey { get; set; } = string.Empty;
    public string FirmaIntegridad { get; set; } = string.Empty;
    public string RedirectUrl { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public string CheckoutVposUrl { get; set; } = string.Empty;
    public string UrlPagoCompleta { get; set; } = string.Empty;
    public string NombreComprador { get; set; } = string.Empty;
    public string CorreoComprador { get; set; } = string.Empty;
    public string? TelefonoComprador { get; set; }
}

public class CompraIniciadaResultVm : WompiCheckoutVm
{
}
