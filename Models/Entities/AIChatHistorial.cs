using System;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Log de consultas realizadas al asistente IA
public partial class AIChatHistorial
{
    public long AIChatHistorialId { get; set; } // PK (BIGINT)
    public string Pregunta { get; set; } = ""; // Texto de la consulta
    public string Respuesta { get; set; } = ""; // Respuesta del asistente
    public int TiempoRespuestaMs { get; set; } // Tiempo de respuesta en ms
    public string Estado { get; set; } = "Exitosa"; // Exitosa / Bloqueada / Error
    public string? TemaDetectado { get; set; } // Tema inferido de la consulta
    public string? SessionId { get; set; } // ID de sesión del visitante
    public int? VisitanteId { get; set; } // FK opcional a Visitantes
    public DateTime FechaConsulta { get; set; } = DateTime.UtcNow;

    public virtual Visitante? Visitante { get; set; } // Nav property
}
