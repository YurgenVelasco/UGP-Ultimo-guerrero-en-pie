using System;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Asistente registrado para un ticket individual (relación 1:1)
public partial class Asistente
{
    public int AsistenteId { get; set; }
    public int TicketId { get; set; } // FK 1:1 con Ticket
    public string Nombre { get; set; } = null!;
    public string Apellido { get; set; } = null!;
    public string TipoDocumento { get; set; } = null!;
    public string NumeroDocumento { get; set; } = null!;
    public string Correo { get; set; } = null!;
    public string? Celular { get; set; }
    public DateOnly? FechaNacimiento { get; set; }

    public virtual Ticket Ticket { get; set; } = null!;
}
