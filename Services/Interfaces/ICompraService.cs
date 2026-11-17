using System.Security.Claims;
using System.Threading.Tasks;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;
using UGP_ultimo_guerrero_en_pie_.Models.ViewModels;

namespace UGP_ultimo_guerrero_en_pie_.Services.Interfaces;

public interface ICompraService
{
    Task<WompiCheckoutVm> CrearCompraPendienteAsync(CheckoutFormVm modelo);
    Task<WompiCheckoutVm> IniciarCompraAsync(CheckoutFormVm modelo);
    Task<string> VerificarPagoWompiAsync(ClaimsPrincipal usuario, int compraId, string transactionId, CancellationToken cancellationToken = default);
    Task<Compra?> ObtenerCompraPorReferenciaAsync(ClaimsPrincipal usuario, string referencia);
}
