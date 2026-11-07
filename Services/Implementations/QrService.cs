using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using QRCoder;
using UGP_ultimo_guerrero_en_pie_.Services.Interfaces;

namespace UGP_ultimo_guerrero_en_pie_.Services.Implementations;

public class QrService : IQrService
{
    private readonly string _secretKey;
    private readonly string _issuer;

    public QrService(IConfiguration configuration)
    {
        _secretKey = configuration["TicketSecurity:SecretKey"] 
            ?? "UGP-SuperSecret-TicketKey-2026-Grappling-Production!";
        _issuer = configuration["TicketSecurity:Issuer"] 
            ?? "UGP-Promotion";
    }

    public byte[] GenerarQr(string contenido)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.Q);
        using var bitmap = new BitmapByteQRCode(data);
        return bitmap.GetGraphic(20);
    }

    public string GenerarTokenHash(long ticketSequenceId, DateTime fecha, string codigoUnico)
    {
        var payload = $"{_issuer}|{ticketSequenceId}|{fecha:yyyyMMddHHmmss}|{codigoUnico}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public bool ValidarTokenHash(long ticketSequenceId, DateTime fecha, string codigoUnico, string hash)
    {
        if (string.IsNullOrWhiteSpace(hash)) return false;
        var expectedHash = GenerarTokenHash(ticketSequenceId, fecha, codigoUnico);
        var hashBytes = Encoding.UTF8.GetBytes(hash.ToLowerInvariant());
        var expectedBytes = Encoding.UTF8.GetBytes(expectedHash);
        return CryptographicOperations.FixedTimeEquals(hashBytes, expectedBytes);
    }
}
