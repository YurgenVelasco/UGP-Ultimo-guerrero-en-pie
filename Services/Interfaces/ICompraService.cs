using System.Threading.Tasks;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;
using UGP_ultimo_guerrero_en_pie_.Models.ViewModels;

namespace UGP_ultimo_guerrero_en_pie_.Services.Interfaces;

public interface ICompraService
{
    Task<WompiCheckoutVm> CrearCompraPendienteAsync(CheckoutFormVm modelo);
    Task<WompiCheckoutVm> IniciarCompraAsync(CheckoutFormVm modelo);
    Task<ResultadoCompraVm> ConfirmarPagoAsync(string referencia, string transactionId, string estado);
    Task<Compra?> ObtenerCompraPorReferenciaAsync(string referencia);
    Task<bool> AprobarCompraYGenerarTicketsAsync(int compraId);
}
