using System.Collections.Generic;
using System.Threading.Tasks;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;

namespace UGP_ultimo_guerrero_en_pie_.Services.Interfaces;

public interface ITicketPdfService
{
    byte[] GenerarTicket(Ticket ticket, Evento evento, Asistente asistente, byte[] qrImage);
    byte[] GenerarTicketsCompra(List<Ticket> tickets, Evento evento, Compra compra, Visitante visitante);
    Task<byte[]> GenerarPdfTicketAsync(int ticketId);
    Task<byte[]> GenerarPdfOrdenAsync(int compraId);
}
