using System.ComponentModel.DataAnnotations;

namespace UGP_ultimo_guerrero_en_pie_.Models.ViewModels;

public sealed class AdminEventForm
{
    [Required(ErrorMessage="El nombre es obligatorio."), StringLength(100, ErrorMessage="El nombre admite hasta 100 caracteres.")] public string Nombre { get; set; } = "";
    public DateOnly Fecha { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public TimeOnly Hora { get; set; } = new(18, 0);
    [Required(ErrorMessage="El lugar es obligatorio."), StringLength(200, ErrorMessage="El lugar admite hasta 200 caracteres.")] public string Lugar { get; set; } = "";
    [StringLength(4000, ErrorMessage="La descripción admite hasta 4000 caracteres.")] public string? Descripcion { get; set; }
    [StringLength(255, ErrorMessage="La dirección de imagen admite hasta 255 caracteres.")] public string? ImagenUrl { get; set; }
    [Range(1, 1000000, ErrorMessage="El aforo debe estar entre 1 y 1.000.000.")] public int CapacidadMaxima { get; set; } = 100;
    [Required(ErrorMessage="Selecciona el estado del evento.")] public string Estado { get; set; } = "Programado";
    public string Version { get; set; } = "";
}
public sealed class AdminLocalityForm
{
    [Required(ErrorMessage="El nombre es obligatorio."), StringLength(50, ErrorMessage="El nombre admite hasta 50 caracteres.")] public string Nombre { get; set; } = "";
    [Range(typeof(decimal), "0.01", "99999999.99", ParseLimitsInInvariantCulture = true, ErrorMessage="El precio debe estar entre 0,01 y 99.999.999,99 COP.")] public decimal Precio { get; set; }
    [Range(1, 1000000, ErrorMessage="La capacidad debe estar entre 1 y 1.000.000.")] public int Capacidad { get; set; } = 100;
    public string Version { get; set; } = "";
}
public sealed class AdminAssociationForm
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una localidad.")] public int TipoEntradaId { get; set; }
}
