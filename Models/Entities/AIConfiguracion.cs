using System;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Configuración general del Chat IA (única fila activa)
public partial class AIConfiguracion
{
    public int AIConfiguracionId { get; set; } // PK
    public bool ChatActivo { get; set; } = true; // Chat habilitado/inhabilitado
    public bool MostrarBoton { get; set; } = true; // Mostrar botón flotante
    public string NombreAsistente { get; set; } = "Asistente UGP"; // Nombre visible
    public string MensajeBienvenida { get; set; } = ""; // Mensaje al iniciar chat
    public string MensajeDespedida { get; set; } = ""; // Mensaje al cerrar chat
    public string? AvatarUrl { get; set; } // URL del avatar del asistente
    public string MensajeBloqueo { get; set; } = ""; // Respuesta cuando se bloquea un mensaje
    public string PromptSistema { get; set; } = ""; // Prompt del sistema para el modelo
    public string Tono { get; set; } = "Profesional"; // Tono de respuesta
    public int LongitudMaxima { get; set; } = 500; // Máx. longitud de respuesta
    public string Formalidad { get; set; } = "Formal"; // Nivel de formalidad
    public string OllamaUrl { get; set; } = "http://localhost:11434"; // URL del servidor Ollama
    public string Modelo { get; set; } = "deepseek-r1:1.5b"; // Modelo activo
    public decimal Temperatura { get; set; } = 0.70m; // Creatividad del modelo (0-2)
    public int MaxTokens { get; set; } = 2048; // Tokens máximos por respuesta
    public int TimeoutSegundos { get; set; } = 120; // Timeout de conexión a Ollama
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;
}
