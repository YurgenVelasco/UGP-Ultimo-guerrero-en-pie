using UGP_ultimo_guerrero_en_pie_.Models.Entities;

namespace UGP_ultimo_guerrero_en_pie_.Services.Implementations;

// Política compartida por el consumidor SQL y las pruebas aisladas.
// Se aplica exclusivamente bajo el bloqueo SQL de la transacción Wompi.
public static class ConciliacionPolicy
{
    public static void Notificar(TransaccionPago pago, DateTime ahora, bool duplicada)
    {
        if (duplicada) return;
        pago.VersionNotificada = checked(pago.VersionNotificada + 1);
        pago.SiguienteConciliacionUtc = ahora;
    }

    public static bool EsObsoleta(TransaccionPago pago, long version) => version < pago.VersionProcesada;

    public static bool ConservarEstado(string actual, string observado) =>
        actual == "APPROVED" ||
        (actual is "DECLINED" or "VOIDED" or "ERROR" && observado != "APPROVED");

    public static void Completar(TransaccionPago pago, long version, DateTime ahora, DateTime? siguiente)
    {
        if (EsObsoleta(pago, version)) return;
        pago.VersionProcesada = Math.Max(pago.VersionProcesada, version);
        // Una respuesta iniciada antes del último webhook no consume ese webhook.
        pago.SiguienteConciliacionUtc = pago.VersionNotificada > version ? ahora : siguiente;
    }
}
