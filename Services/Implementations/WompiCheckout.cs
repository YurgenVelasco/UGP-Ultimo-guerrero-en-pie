using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;

namespace UGP_ultimo_guerrero_en_pie_.Services.Implementations;

public class WompiCheckout(IConfiguration configuration, IHttpClientFactory clients)
{
    public string PublicKey => configuration["Wompi:PublicKey"] ?? "";
    public string Currency => configuration["Wompi:Currency"] ?? "COP";

    public string EnvironmentName => configuration["Wompi:Environment"] ?? "Sandbox";
    public bool ReservationsEnabled => configuration.GetValue<bool>("Reservas:Habilitadas");
    public bool EventsConfigured => HasKey(configuration["Wompi:EventsSecret"],
        EnvironmentName == "Sandbox" ? "test_events_" : "prod_events_");

    public bool IsConfigured => EnvironmentName switch
    {
        "Sandbox" => HasKeys("pub_test_", "test_integrity_", "prv_test_"),
        "Production" => HasKeys("pub_prod_", "prod_integrity_", "prv_prod_"),
        _ => false
    };

    private bool HasKeys(string publicPrefix, string integrityPrefix, string privatePrefix) =>
        HasKey(PublicKey, publicPrefix) &&
        HasKey(configuration["Wompi:IntegritySecret"], integrityPrefix) &&
        HasKey(configuration["Wompi:PrivateKey"], privatePrefix) &&
        Currency == "COP";

    private static bool HasKey(string? value, string prefix) =>
        value is not null && value.Length > prefix.Length &&
        value.StartsWith(prefix, StringComparison.Ordinal) && !value.Any(char.IsWhiteSpace);

    public static string Reference(Compra compra) =>
        FormattableString.Invariant($"UGP-TX-{compra.FechaCompra ?? DateTime.UnixEpoch:yyyyMMdd}-{compra.CompraId:D6}");

    public static long AmountInCents(Compra compra) => checked((long)(compra.MontoTotal * 100m));

    public string Sign(string reference, long amountInCents, string? expirationTime = null)
    {
        var payload = reference + amountInCents.ToString(CultureInfo.InvariantCulture) +
            Currency + expirationTime + configuration["Wompi:IntegritySecret"];
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    public static string Expiration(DateTime utc) => utc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

    // Nunca se confía en el estado del webhook para aprobar: después de autenticar
    // se consulta también el ID con la API privada y se coteja la orden persistida.
    public bool ValidateEvent(JsonElement root, out string reference, out string transactionId)
    {
        reference = transactionId = "";
        try
        {
            if (!EventsConfigured || ReadString(root, "event") != "transaction.updated" ||
                ReadString(root, "environment") != (EnvironmentName == "Sandbox" ? "test" : "prod")) return false;
            var data = root.GetProperty("data");
            var signature = root.GetProperty("signature");
            var properties = signature.GetProperty("properties").EnumerateArray().Select(p => p.GetString()!).ToArray();
            if (properties.Length is < 3 or > 20 ||
                !new[] { "transaction.id", "transaction.status", "transaction.amount_in_cents" }.All(properties.Contains)) return false;
            var payload = new StringBuilder();
            foreach (var path in properties)
            {
                if (path is null || path.Length > 150) return false;
                var value = data;
                foreach (var part in path.Split('.')) value = value.GetProperty(part);
                if (value.ValueKind is not (JsonValueKind.String or JsonValueKind.Number)) return false;
                payload.Append(value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText());
            }
            payload.Append(root.GetProperty("timestamp").GetInt64().ToString(CultureInfo.InvariantCulture));
            payload.Append(configuration["Wompi:EventsSecret"]);
            var supplied = Convert.FromHexString(signature.GetProperty("checksum").GetString()!);
            if (!CryptographicOperations.FixedTimeEquals(supplied, SHA256.HashData(Encoding.UTF8.GetBytes(payload.ToString())))) return false;
            var transaction = data.GetProperty("transaction");
            reference = ReadString(transaction, "reference") ?? "";
            transactionId = ReadString(transaction, "id") ?? "";
            return reference.Length is > 0 and <= 100 && ValidTransactionId(transactionId);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or FormatException or ArgumentException)
        {
            return false;
        }
    }

    public static bool ValidTransactionId(string id) => id.Length is > 0 and <= 100 &&
        id.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    public async Task<PagoWompi?> GetPaymentAsync(string transactionId, CancellationToken cancellationToken)
    {
        if (!IsConfigured || transactionId.Length is < 1 or > 100 ||
            transactionId.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_'))
        {
            return null;
        }

        var host = EnvironmentName == "Sandbox" ? "sandbox.wompi.co" : "production.wompi.co";
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://{host}/v1/transactions/{transactionId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuration["Wompi:PrivateKey"]);
        using var client = clients.CreateClient("Wompi");
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var body = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
        if (body.RootElement.ValueKind != JsonValueKind.Object ||
            !body.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty("amount_in_cents", out var amount) || amount.ValueKind != JsonValueKind.Number ||
            !amount.TryGetInt64(out var cents) ||
            ReadString(data, "id") != transactionId)
        {
            return null;
        }

        var status = ReadString(data, "status");
        var reference = ReadString(data, "reference");
        var currency = ReadString(data, "currency");
        return status is "APPROVED" or "PENDING" or "DECLINED" or "VOIDED" or "ERROR" &&
            reference is { Length: > 0 and <= 100 } && currency is { Length: 3 }
            ? new PagoWompi(transactionId, reference, cents, currency, status) : null;
    }

    private static string? ReadString(JsonElement data, string name) =>
        data.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}

public sealed record PagoWompi(string Id, string Referencia, long Centavos, string Moneda, string Estado);
