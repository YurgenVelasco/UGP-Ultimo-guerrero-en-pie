using System;

namespace UGP_ultimo_guerrero_en_pie_.Services.Interfaces;

public interface IQrService
{
    byte[] GenerarQr(string contenido);
    string GenerarTokenHash(long ticketSequenceId, DateTime fecha, string codigoUnico);
    bool ValidarTokenHash(long ticketSequenceId, DateTime fecha, string codigoUnico, string hash);
}
