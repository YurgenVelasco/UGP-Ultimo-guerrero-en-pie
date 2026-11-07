using System.Collections.Generic;
using System.Threading.Tasks;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;
using UGP_ultimo_guerrero_en_pie_.Models.ViewModels;

namespace UGP_ultimo_guerrero_en_pie_.Services.Interfaces;

public interface ITicketService
{
    Task<List<Ticket>> GenerarTickets(Compra compra, List<DetalleCompraVm> detalles, List<AsistenteVm> asistentes);
    Task<TicketVm?> ObtenerTicket(int ticketId);
    Task<ValidarTicketVm?> ValidarCodigo(string codigoUnico);
    Task<ValidarTicketVm?> ValidarQrContenido(string qrContent);
    Task<ValidarTicketVm> UsarTicket(int ticketId);
    Task<byte[]> RegenerarQr(int ticketId);
    Task<long> ObtenerSiguienteFolioAsync();
}
