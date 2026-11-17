using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UGP_ultimo_guerrero_en_pie_.Models.ViewModels;

public class AsistenteInputVm
{
    [Required(ErrorMessage = "El nombre completo del asistente es requerido")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de documento es requerido")]
    [RegularExpression(CheckoutFormVm.FormatoTipoDocumento, ErrorMessage = "Selecciona un tipo de documento válido.")]
    public string TipoDocumento { get; set; } = "CC";

    [Required(ErrorMessage = "El número de documento de identidad es requerido")]
    [StringLength(30, MinimumLength = 4, ErrorMessage = "Documento no válido")]
    public string DocumentoIdentidad { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es requerido")]
    [EmailAddress(ErrorMessage = "Formato de correo electrónico no válido")]
    [RegularExpression(CheckoutFormVm.FormatoCorreo, ErrorMessage = "Ingresa un correo electrónico válido.")]
    [StringLength(150, ErrorMessage = "El correo no puede superar 150 caracteres.")]
    public string CorreoElectronico { get; set; } = string.Empty;

    [RegularExpression(CheckoutFormVm.FormatoTelefono, ErrorMessage = CheckoutFormVm.ErrorTelefono)]
    public string? Telefono { get; set; }
}

public class CheckoutFormVm
{
    internal const string FormatoTipoDocumento = "CC|CE|TI|PAS|NIT";
    internal const string FormatoCorreo = @"[\p{L}\p{N}!#$%&'*+/=?^_`{|}~-]+(?:\.[\p{L}\p{N}!#$%&'*+/=?^_`{|}~-]+)*@[\p{L}\p{N}](?:[\p{L}\p{N}-]*[\p{L}\p{N}])?(?:\.[\p{L}\p{N}](?:[\p{L}\p{N}-]*[\p{L}\p{N}])?)+";
    internal const string FormatoTelefono = @"(?=(?:[^0-9]*[0-9]){7,15}[^0-9]*$)\+?[0-9 ().-]{1,29}";
    internal const string ErrorTelefono = "Ingresa entre 7 y 15 dígitos. Puedes incluir el prefijo internacional.";
    public const int MaximoBoletos = 10;

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un evento disponible.")]
    public int EventoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una localidad disponible.")]
    public int TipoEntradaId { get; set; }

    [Range(1, MaximoBoletos, ErrorMessage = "La cantidad permitida es entre 1 y 10 boletos por compra.")]
    public int Cantidad { get; set; } = 1;

    // Datos del comprador principal
    [Required(ErrorMessage = "El nombre del comprador es requerido")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres")]
    public string NombreComprador { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo del comprador es requerido")]
    [EmailAddress(ErrorMessage = "Formato de correo no válido")]
    [RegularExpression(FormatoCorreo, ErrorMessage = "Ingresa un correo electrónico válido.")]
    [StringLength(150, ErrorMessage = "El correo no puede superar 150 caracteres.")]
    public string CorreoComprador { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de documento es requerido")]
    [RegularExpression(FormatoTipoDocumento, ErrorMessage = "Selecciona un tipo de documento válido.")]
    public string TipoDocumento { get; set; } = "CC";

    [Required(ErrorMessage = "El documento de identidad es requerido")]
    [StringLength(30, MinimumLength = 4, ErrorMessage = "Documento no válido")]
    public string DocumentoComprador { get; set; } = string.Empty;

    [RegularExpression(FormatoTelefono, ErrorMessage = ErrorTelefono)]
    public string? TelefonoComprador { get; set; }

    // Lista de asistentes para cada boleto individual
    public List<AsistenteInputVm> Asistentes { get; set; } = new();

    public decimal MontoTotal { get; set; }

    public void Normalizar()
    {
        NombreComprador = NombreComprador?.Trim() ?? "";
        CorreoComprador = CorreoComprador?.Trim().ToLowerInvariant() ?? "";
        DocumentoComprador = DocumentoComprador?.Trim() ?? "";
        TelefonoComprador = string.IsNullOrWhiteSpace(TelefonoComprador) ? null : TelefonoComprador.Trim();

        foreach (var asistente in Asistentes ?? [])
        {
            if (asistente is null) continue;
            asistente.NombreCompleto = asistente.NombreCompleto?.Trim() ?? "";
            asistente.CorreoElectronico = asistente.CorreoElectronico?.Trim().ToLowerInvariant() ?? "";
            asistente.DocumentoIdentidad = asistente.DocumentoIdentidad?.Trim() ?? "";
            asistente.Telefono = string.IsNullOrWhiteSpace(asistente.Telefono) ? null : asistente.Telefono.Trim();
        }
    }

    public IEnumerable<(object Modelo, ValidationResult Error)> Validar()
    {
        var errores = new List<ValidationResult>();
        Validator.TryValidateObject(this, new ValidationContext(this), errores, validateAllProperties: true);
        foreach (var error in errores)
            yield return (this, error);

        if (Asistentes is null || Asistentes.Count != Cantidad || Asistentes.Exists(a => a is null))
        {
            yield return (this, new ValidationResult("Completa los datos de un asistente por boleto.", [nameof(Asistentes)]));
            yield break;
        }

        foreach (var asistente in Asistentes)
        {
            errores.Clear();
            Validator.TryValidateObject(asistente, new ValidationContext(asistente), errores, validateAllProperties: true);
            foreach (var error in errores)
                yield return (asistente, error);
        }
    }
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
