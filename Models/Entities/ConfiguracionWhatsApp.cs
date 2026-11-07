using System;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Configura el comportamiento y disponibilidad del widget de chat de WhatsApp
public partial class ConfiguracionWhatsApp
{
    public int ConfiguracionWhatsAppId { get; set; }

    public bool Activo { get; set; } = true;

    // Tooltip mostrado al pasar el mouse sobre el botón del widget
    public string TooltipTexto { get; set; } = "¿Necesitas ayuda? Escríbenos por WhatsApp.";

    // Mensaje predefinido que el usuario ve en el chat
    public string MensajePredeterminado { get; set; } = "Hola.\nEstoy interesado en conocer más información sobre los eventos de Ultimate Grappling Promotion.\n¿Podrían ayudarme?";

    // Respuesta automática enviada fuera del horario laboral
    public string? MensajeFueraHorario { get; set; }

    // Hora de inicio del horario laboral
    public TimeSpan? HoraInicio { get; set; }

    // Hora de fin del horario laboral
    public TimeSpan? HoraFin { get; set; }

    public bool MostrarEnAdmin { get; set; } = true;

    public bool MostrarEnPublico { get; set; } = true;

    // Lista JSON de rutas de página donde el widget es visible
    public string? PaginasMostrar { get; set; }

    public DateTime FechaActualizacion { get; set; }
}
