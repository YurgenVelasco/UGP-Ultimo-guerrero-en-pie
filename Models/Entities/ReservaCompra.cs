namespace UGP_ultimo_guerrero_en_pie_.Models.Entities;

public sealed class ReservaCompra
{
    public int CompraId { get; set; }
    public DateTime CreadaUtc { get; set; }
    public DateTime ExpiraUtc { get; set; }
    public string Estado { get; set; } = "Vigente";
    public DateTime? ConfirmadaUtc { get; set; }
    public DateTime? LiberadaUtc { get; set; }
    public string? MotivoLiberacion { get; set; }
    public Guid OperacionId { get; set; }
    public string? AsistentesJson { get; set; }
    public DateTime? ReintentarLiberacionUtc { get; set; }
    public Compra Compra { get; set; } = null!;
}

// Un intento se persiste ANTES de entregar el checkout; Wompi puede crear varias
// transacciones para una referencia (por ejemplo, reintentos del proveedor).
public sealed class IntentoPago
{
    public Guid Id { get; set; }
    public int CompraId { get; set; }
    public string Referencia { get; set; } = "";
    public string Entorno { get; set; } = "";
    public long Centavos { get; set; }
    public string Moneda { get; set; } = "COP";
    public DateTime CreadoUtc { get; set; }
    public DateTime? UltimaVerificacionUtc { get; set; }
    public DateTime? SiguienteConciliacionUtc { get; set; }
    public string Estado { get; set; } = "SinTransaccion";
    public ReservaCompra Reserva { get; set; } = null!;
}

public sealed class TransaccionPago
{
    public Guid Id { get; set; }
    public Guid? IntentoPagoId { get; set; }
    public string Entorno { get; set; } = "";
    public string WompiId { get; set; } = "";
    public string Estado { get; set; } = "SinVerificar";
    public DateTime RegistradaUtc { get; set; }
    public DateTime? VerificadaUtc { get; set; }
    public DateTime? SiguienteConciliacionUtc { get; set; }
    public string? UltimoResultado { get; set; }
    public long VersionNotificada { get; set; }
    public long VersionProcesada { get; set; }
    public IntentoPago? Intento { get; set; }
}

public sealed class NotificacionPago
{
    public Guid Id { get; set; }
    public Guid TransaccionPagoId { get; set; }
    public string Huella { get; set; } = "";
    public DateTime RecibidaUtc { get; set; }
    public TransaccionPago Transaccion { get; set; } = null!;
}

public sealed class VerificacionPago
{
    public Guid Id { get; set; }
    public Guid TransaccionPagoId { get; set; }
    public long VersionConsultada { get; set; }
    public DateTime ConsultadaUtc { get; set; }
    public string? EstadoObservado { get; set; }
    public string Resultado { get; set; } = "";
    public TransaccionPago Transaccion { get; set; } = null!;
}

public sealed class IncidenciaPago
{
    public Guid Id { get; set; }
    public Guid TransaccionPagoId { get; set; }
    public string Motivo { get; set; } = "";
    public string Estado { get; set; } = "Pendiente";
    public DateTime CreadaUtc { get; set; }
    public DateTime? ResueltaUtc { get; set; }
    public string? ResueltaPor { get; set; }
    public string? EvidenciaResolucion { get; set; }
    public TransaccionPago Transaccion { get; set; } = null!;
}
