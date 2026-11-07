using System.ComponentModel.DataAnnotations;

namespace UGP_ultimo_guerrero_en_pie_.Models.ViewModels;

public class DetalleCompraVm
{
    public int TipoEntradaId { get; set; }
    public int Cantidad { get; set; }
    public int? VenueZoneId { get; set; }
}

public class AsistenteVm
{
    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    public string Apellido { get; set; } = string.Empty;

    [Required]
    public string TipoDocumento { get; set; } = "CC";

    [Required]
    public string Documento { get; set; } = string.Empty;

    [Required]
    public string Celular { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Correo { get; set; } = string.Empty;

    public string? FechaNacimiento { get; set; }
}

public class TicketVm
{
    public int TicketId { get; set; }
    public string CodigoUnico { get; set; } = string.Empty;
    public string CodigoQr { get; set; } = string.Empty;
    public string? TipoTicket { get; set; }
    public decimal? Precio { get; set; }
    public string? Estado { get; set; }
    public string EventoNombre { get; set; } = string.Empty;
    public string? FechaEvento { get; set; }
    public string? HoraEvento { get; set; }
    public string? Lugar { get; set; }
    public string CompradorNombre { get; set; } = string.Empty;
}

public class ValidarTicketVm
{
    public bool Existe { get; set; }
    public string? EstadoTicket { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public int TicketId { get; set; }
    public string? CodigoUnico { get; set; }
    public string? TipoTicket { get; set; }
    public decimal Precio { get; set; }
    public string? EventoNombre { get; set; }
    public string? FechaEvento { get; set; }
    public string? HoraEvento { get; set; }
    public string? Lugar { get; set; }
    public string? CompradorNombre { get; set; }
    public string? AsistenteNombre { get; set; }
    public string? QrPath { get; set; }
}

public class TicketCompraVm
{
    public int TicketId { get; set; }
    public string CodigoUnico { get; set; } = string.Empty;
    public string? TipoTicket { get; set; }
    public decimal? Precio { get; set; }
    public string? Estado { get; set; }
    public string AsistenteNombre { get; set; } = string.Empty;
    public string AsistenteDocumento { get; set; } = string.Empty;
    public string? QrPath { get; set; }
}

public class ResultadoCompraVm
{
    public bool Exitosa { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public int? CompraId { get; set; }
    public decimal MontoTotal { get; set; }
    public int CantidadTickets { get; set; }
    public List<string>? CodigosTickets { get; set; }
}

public class ConfirmacionCompraVm
{
    public int CompraId { get; set; }
    public string EventoNombre { get; set; } = string.Empty;
    public string? FechaEvento { get; set; }
    public string? HoraEvento { get; set; }
    public string? Lugar { get; set; }
    public string CompradorNombre { get; set; } = string.Empty;
    public string CompradorCorreo { get; set; } = string.Empty;
    public decimal MontoTotal { get; set; }
    public string EstadoPago { get; set; } = string.Empty;
    public DateTime? FechaCompra { get; set; }
    public List<TicketCompraVm> Tickets { get; set; } = new();
    public bool MismoComprador { get; set; }
}

public class CompraResumenVm
{
    public int CompraId { get; set; }
    public string EventoNombre { get; set; } = string.Empty;
    public string? FechaEvento { get; set; }
    public string Comprador { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public decimal MontoTotal { get; set; }
    public string EstadoPago { get; set; } = string.Empty;
    public DateTime? FechaCompra { get; set; }
    public int CantidadTickets { get; set; }
}

public class CompraDetalleVm
{
    public int CompraId { get; set; }
    public string EventoNombre { get; set; } = string.Empty;
    public string? FechaEvento { get; set; }
    public string? HoraEvento { get; set; }
    public string? Lugar { get; set; }
    public string CompradorNombre { get; set; } = string.Empty;
    public string CompradorCorreo { get; set; } = string.Empty;
    public string? CompradorTelefono { get; set; }
    public decimal MontoTotal { get; set; }
    public string EstadoPago { get; set; } = string.Empty;
    public string MetodoPago { get; set; } = string.Empty;
    public DateTime? FechaCompra { get; set; }
    public List<TicketCompraVm> Tickets { get; set; } = new();
    public bool CorreoEnviado { get; set; }
}

public class ZonaCompraVm
{
    public int VenueZoneId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Disponibles { get; set; }
    public string Color { get; set; } = "#D4AF37";
    public string? SVGPathId { get; set; }
}
