using System;
using System.Collections.Generic;

namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

// Relación muchos a muchos: tipos de boleto disponibles para un evento
public partial class EventoTipoEntrada
{
    public int EventoTipoId { get; set; }

    public int EventoId { get; set; }

    public int TipoEntradaId { get; set; }

    public virtual Evento Evento { get; set; } = null!;

    public virtual TipoEntrada TipoEntrada { get; set; } = null!;
}
