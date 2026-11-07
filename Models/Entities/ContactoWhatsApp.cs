using System;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Agente de contacto de WhatsApp mostrado en el widget de chat
public partial class ContactoWhatsApp
{
    public int ContactoWhatsAppId { get; set; }

    public string Nombre { get; set; } = null!;

    // Número telefónico con código de país (ej. "+521234567890")
    public string Telefono { get; set; } = null!;

    // Indicador del contacto predeterminado para chats entrantes
    public bool EsPrincipal { get; set; }

    public bool Activo { get; set; } = true;

    // Orden de visualización en la lista de contactos
    public int Orden { get; set; }

    public DateTime FechaCreacion { get; set; }
}
