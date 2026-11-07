using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UGP_ultimo_guerrero_en_pie_.Data;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;
using UGP_ultimo_guerrero_en_pie_.Models.Enums;
using UGP_ultimo_guerrero_en_pie_.Models.ViewModels;
using UGP_ultimo_guerrero_en_pie_.Services.Interfaces;

namespace UGP_ultimo_guerrero_en_pie_.Services.Implementations;

public class CompraService : ICompraService
{
    private readonly ApplicationDbContext _db;
    private readonly ITicketService _ticketService;
    private readonly IConfiguration _config;
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, List<AsistenteInputVm>> _asistentesPendientes = new();

    public CompraService(ApplicationDbContext db, ITicketService ticketService, IConfiguration config)
    {
        _db = db;
        _ticketService = ticketService;
        _config = config;
    }

    public async Task<WompiCheckoutVm> CrearCompraPendienteAsync(CheckoutFormVm modelo)
    {
        if (modelo.Cantidad < 1 || modelo.Cantidad > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(modelo.Cantidad), "La cantidad de boletos debe estar entre 1 y 10.");
        }

        try
        {
            // Ejecución resiliente con estrategia de reintento transitorio de EF Core
            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                // Transacción serializable para prevenir sobreventa por concurrencia
                await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                try
                {
                    var evento = await _db.Eventos
                        .FirstOrDefaultAsync(e => e.EventoId == modelo.EventoId);

                    // Si no se encuentra por EventoId específico, intentar con el primer evento activo o disponible
                    if (evento == null)
                    {
                        evento = await _db.Eventos
                            .OrderBy(e => e.Fecha)
                            .FirstOrDefaultAsync(e => e.EstadoEvento == "Activo" || e.EstadoEvento == "Próximo")
                            ?? await _db.Eventos.FirstOrDefaultAsync();
                    }

                    if (evento == null)
                    {
                        throw new InvalidOperationException("No se encontró ningún evento disponible en la boletería oficial de UGP.");
                    }

                    if (evento.TicketsDisponibles < modelo.Cantidad)
                    {
                        throw new InvalidOperationException($"Aforo insuficiente. Solo quedan {evento.TicketsDisponibles} boletos disponibles.");
                    }

                    var tipoEntrada = await _db.TiposEntrada
                        .FirstOrDefaultAsync(t => t.TipoEntradaId == modelo.TipoEntradaId);

                    if (tipoEntrada == null)
                    {
                        tipoEntrada = await _db.EventoTiposEntrada
                            .Where(ete => ete.EventoId == evento.EventoId)
                            .Select(ete => ete.TipoEntrada)
                            .FirstOrDefaultAsync()
                            ?? await _db.TiposEntrada.FirstOrDefaultAsync();
                    }

                    if (tipoEntrada == null)
                    {
                        throw new InvalidOperationException("La localidad o tipo de entrada seleccionado no existe o no está disponible.");
                    }

                    // Registrar o actualizar visitante (comprador)
                    var emailNormalizado = (modelo.CorreoComprador ?? string.Empty).ToLower().Trim();
                    var visitante = await _db.Visitantes
                        .FirstOrDefaultAsync(v => v.Correo.ToLower() == emailNormalizado);

                    if (visitante == null)
                    {
                        visitante = new Visitante
                        {
                            Nombre = modelo.NombreComprador,
                            Correo = emailNormalizado,
                            Telefono = modelo.TelefonoComprador,
                            FechaRegistro = DateTime.UtcNow
                        };
                        _db.Visitantes.Add(visitante);
                        await _db.SaveChangesAsync();
                    }
                    else
                    {
                        visitante.Nombre = modelo.NombreComprador;
                        visitante.Telefono = modelo.TelefonoComprador ?? visitante.Telefono;
                    }

                    // Método de pago: Wompi
                    var metodoPago = await _db.MetodosPagos
                        .FirstOrDefaultAsync(m => m.Nombre == "Wompi");

                    if (metodoPago == null)
                    {
                        metodoPago = new MetodoPago { Nombre = "Wompi" };
                        _db.MetodosPagos.Add(metodoPago);
                        await _db.SaveChangesAsync();
                    }

                    var subtotal = tipoEntrada.Precio * modelo.Cantidad;

                    // Registrar Compra en estado Pendiente
                    var compra = new Compra
                    {
                        EventoId = evento.EventoId,
                        VisitanteId = visitante.VisitanteId,
                        MetodoPagoId = metodoPago.MetodoPagoId,
                        FechaCompra = DateTime.UtcNow,
                        MontoTotal = subtotal,
                        EstadoPago = EstadoCompra.Pendiente.ToString()
                    };

                    _db.Compras.Add(compra);
                    await _db.SaveChangesAsync();

                    if (modelo.Asistentes?.Any() == true)
                    {
                        _asistentesPendientes[compra.CompraId] = modelo.Asistentes;
                    }

                    // Registrar Detalle de la Compra
                    var detalle = new DetalleCompra
                    {
                        CompraId = compra.CompraId,
                        TipoEntradaId = tipoEntrada.TipoEntradaId,
                        Cantidad = modelo.Cantidad,
                        PrecioUnitario = tipoEntrada.Precio,
                        Subtotal = subtotal
                    };

                    _db.DetalleCompras.Add(detalle);

                    // Reducción atómica del aforo disponible
                    evento.TicketsDisponibles -= modelo.Cantidad;
                    if (evento.TicketsDisponibles < 0) evento.TicketsDisponibles = 0;

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Generar Referencia Única de Wompi
                    var fecha = DateTime.UtcNow;
                    var referencia = $"UGP-TX-{fecha:yyyyMMdd}-{compra.CompraId:D6}";

                    // Parámetros oficiales de Wompi Checkout Web Dinámico
                    var publicKey = _config["Wompi:PublicKey"] ?? "pub_test_X0zDA9xoKdePuotSdPUxggaGLxDsq4Yt";
                    var integritySecret = _config["Wompi:IntegritySecret"] ?? "test_integrity_SecretKeyWompi2026!";
                    var checkoutBaseUrl = _config["Wompi:CheckoutUrl"] 
                                          ?? _config["Wompi:CheckoutVposUrl"] 
                                          ?? "https://checkout.wompi.co/p/";
                    var moneda = _config["Wompi:Currency"] ?? "COP";
                    var redirectUrl = _config["Wompi:RedirectUrl"] ?? "http://localhost:5145/checkout/confirmacion";

                    var montoEnCentavos = (long)(subtotal * 100);

                    // Firma de Integridad Wompi: SHA256(referencia + montoEnCentavos + moneda + integritySecret)
                    var rawCadena = $"{referencia}{montoEnCentavos}{moneda}{integritySecret}";
                    var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawCadena));
                    var firmaIntegridad = Convert.ToHexString(hashBytes).ToLowerInvariant();

                    // Construcción de la URL de Wompi Checkout Web Dinámico (/p/)
                    // Este endpoint requiere query parameters oficiales con valor exacto en centavos,
                    // firma de integridad SHA-256 y URL de retorno para redirección automática.
                    var queryParams = new List<string>
                    {
                        $"public-key={Uri.EscapeDataString(publicKey)}",
                        $"currency={Uri.EscapeDataString(moneda)}",
                        $"amount-in-cents={montoEnCentavos}",
                        $"reference={Uri.EscapeDataString(referencia)}",
                        $"signature:integrity={firmaIntegridad}"
                    };

                    if (!string.IsNullOrWhiteSpace(redirectUrl))
                    {
                        queryParams.Add($"redirect-url={Uri.EscapeDataString(redirectUrl)}");
                    }

                    if (!string.IsNullOrWhiteSpace(modelo.CorreoComprador))
                    {
                        queryParams.Add($"customer-data:email={Uri.EscapeDataString(modelo.CorreoComprador.Trim())}");
                    }

                    if (!string.IsNullOrWhiteSpace(modelo.NombreComprador))
                    {
                        queryParams.Add($"customer-data:full-name={Uri.EscapeDataString(modelo.NombreComprador.Trim())}");
                    }

                    if (!string.IsNullOrWhiteSpace(modelo.TelefonoComprador))
                    {
                        var digitsOnly = new string(modelo.TelefonoComprador.Where(char.IsDigit).ToArray());
                        if (digitsOnly.Length >= 7)
                        {
                            if (digitsOnly.StartsWith("57") && digitsOnly.Length == 12)
                            {
                                digitsOnly = digitsOnly.Substring(2);
                            }
                            queryParams.Add("customer-data:phone-number-prefix=%2B57");
                            queryParams.Add($"customer-data:phone-number={digitsOnly}");
                        }
                    }

                    var separador = checkoutBaseUrl.Contains("?") ? "&" : (checkoutBaseUrl.EndsWith("/") ? "?" : "/?");
                    var urlPagoCompleta = $"{checkoutBaseUrl}{separador}{string.Join("&", queryParams)}";

                    return new WompiCheckoutVm
                    {
                        CompraId = compra.CompraId,
                        Referencia = referencia,
                        MontoEnCentavos = montoEnCentavos,
                        Moneda = moneda,
                        PublicKey = publicKey,
                        FirmaIntegridad = firmaIntegridad,
                        RedirectUrl = redirectUrl,
                        CheckoutUrl = urlPagoCompleta,
                        CheckoutVposUrl = checkoutBaseUrl,
                        UrlPagoCompleta = urlPagoCompleta,
                        NombreComprador = modelo.NombreComprador ?? string.Empty,
                        CorreoComprador = modelo.CorreoComprador ?? string.Empty,
                        TelefonoComprador = modelo.TelefonoComprador
                    };
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"No fue posible completar la reserva en la base de datos debido a un problema de conexión transitorio ({ex.Message}). Verifica que SQL Server LocalDB esté activo e inténtalo de nuevo.", ex);
        }
    }

    public Task<WompiCheckoutVm> IniciarCompraAsync(CheckoutFormVm modelo)
    {
        return CrearCompraPendienteAsync(modelo);
    }

    public async Task<bool> AprobarCompraYGenerarTicketsAsync(int compraId)
    {
        var compra = await _db.Compras
            .Include(c => c.Evento)
            .Include(c => c.Visitante)
            .Include(c => c.Tickets).ThenInclude(t => t.Asistente)
            .Include(c => c.DetalleCompras).ThenInclude(d => d.TipoEntrada)
            .FirstOrDefaultAsync(c => c.CompraId == compraId);

        if (compra == null) return false;

        // Si ya está aprobado y tiene tickets, retornar true
        if (compra.EstadoPago == EstadoCompra.Aprobado.ToString() && compra.Tickets.Any())
        {
            return true;
        }

        compra.EstadoPago = EstadoCompra.Aprobado.ToString();

        // Si no tiene tickets emitidos aún, generarlos formalmente con ITicketService
        if (!compra.Tickets.Any())
        {
            var detallesVm = compra.DetalleCompras.Select(d => new DetalleCompraVm
            {
                TipoEntradaId = d.TipoEntradaId,
                Cantidad = d.Cantidad
            }).ToList();

            var totalTickets = detallesVm.Sum(d => d.Cantidad);
            var asistentesVm = new List<AsistenteVm>();

            // Verificar si tenemos los datos ingresados en el formulario de checkout
            if (_asistentesPendientes.TryRemove(compraId, out var listaAsistentes) && listaAsistentes.Any())
            {
                foreach (var a in listaAsistentes)
                {
                    var nombres = (a.NombreCompleto ?? "Asistente").Trim().Split(' ', 2);
                    var nombre = nombres[0];
                    var apellido = nombres.Length > 1 ? nombres[1] : "";

                    asistentesVm.Add(new AsistenteVm
                    {
                        Nombre = nombre,
                        Apellido = apellido,
                        TipoDocumento = string.IsNullOrWhiteSpace(a.TipoDocumento) ? "CC" : a.TipoDocumento,
                        Documento = a.DocumentoIdentidad ?? "000000000",
                        Correo = a.CorreoElectronico ?? compra.Visitante?.Correo ?? "",
                        Celular = a.Telefono ?? compra.Visitante?.Telefono ?? ""
                    });
                }
            }

            // Rellenar con los datos del comprador si faltan asistentes
            while (asistentesVm.Count < totalTickets)
            {
                var partesNom = (compra.Visitante?.Nombre ?? "Asistente").Trim().Split(' ', 2);
                asistentesVm.Add(new AsistenteVm
                {
                    Nombre = partesNom[0],
                    Apellido = partesNom.Length > 1 ? partesNom[1] : "",
                    TipoDocumento = "CC",
                    Documento = "000000000",
                    Correo = compra.Visitante?.Correo ?? "",
                    Celular = compra.Visitante?.Telefono ?? ""
                });
            }

            await _ticketService.GenerarTickets(compra, detallesVm, asistentesVm);
        }
        else
        {
            // Si ya existían tickets en estado pendiente, activarlos
            foreach (var t in compra.Tickets)
            {
                t.Estado = EstadoTicket.Activo.ToString();
            }
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<ResultadoCompraVm> ConfirmarPagoAsync(string referencia, string transactionId, string estado)
    {
        // Extraer CompraId de la referencia UGP-TX-yyyyMMdd-XXXXXX o ID directo
        var partes = referencia.Split('-');
        int compraId;
        if (partes.Length >= 4 && int.TryParse(partes[3], out var idDePartes))
        {
            compraId = idDePartes;
        }
        else if (!int.TryParse(referencia, out compraId))
        {
            return new ResultadoCompraVm
            {
                Exitosa = false,
                Mensaje = "Referencia de pago no válida."
            };
        }

        var compra = await _db.Compras
            .Include(c => c.Evento)
            .Include(c => c.Visitante)
            .Include(c => c.DetalleCompras).ThenInclude(d => d.TipoEntrada)
            .Include(c => c.Tickets)
            .FirstOrDefaultAsync(c => c.CompraId == compraId);

        if (compra == null)
        {
            return new ResultadoCompraVm
            {
                Exitosa = false,
                Mensaje = "Orden de compra no encontrada."
            };
        }

        if (estado.ToUpper() == "APPROVED" || estado.ToUpper() == "APROBADO")
        {
            await AprobarCompraYGenerarTicketsAsync(compraId);

            var ticketsActualizados = await _db.Tickets
                .Where(t => t.CompraId == compraId)
                .ToListAsync();

            return new ResultadoCompraVm
            {
                Exitosa = true,
                CompraId = compra.CompraId,
                MontoTotal = compra.MontoTotal,
                CantidadTickets = ticketsActualizados.Count,
                CodigosTickets = ticketsActualizados.Select(t => t.CodigoUnico).ToList(),
                Mensaje = "Pago aprobado con éxito. Boletos digitales emitidos con firma criptográfica."
            };
        }
        else
        {
            compra.EstadoPago = EstadoCompra.Rechazado.ToString();
            // Restaurar aforo
            if (compra.Evento != null)
            {
                var cantidad = compra.DetalleCompras.Sum(d => d.Cantidad);
                compra.Evento.TicketsDisponibles += cantidad;
            }
            await _db.SaveChangesAsync();

            return new ResultadoCompraVm
            {
                Exitosa = false,
                CompraId = compra.CompraId,
                Mensaje = $"La transacción fue {estado}. Los boletos han sido liberados."
            };
        }
    }

    public async Task<Compra?> ObtenerCompraPorReferenciaAsync(string referencia)
    {
        if (string.IsNullOrWhiteSpace(referencia)) return null;

        referencia = referencia.Trim();

        // 1. Intentar parsear formato UGP-TX-yyyyMMdd-XXXXXX
        int compraId = 0;
        var partes = referencia.Split('-');
        if (partes.Length >= 4 && int.TryParse(partes[3], out var idDePartes))
        {
            compraId = idDePartes;
        }
        else if (int.TryParse(referencia, out var directId))
        {
            compraId = directId;
        }

        Compra? compra = null;
        if (compraId > 0)
        {
            compra = await _db.Compras
                .Include(c => c.Evento).ThenInclude(e => e.Venue)
                .Include(c => c.Visitante)
                .Include(c => c.Tickets).ThenInclude(t => t.Asistente)
                .Include(c => c.DetalleCompras).ThenInclude(d => d.TipoEntrada)
                .FirstOrDefaultAsync(c => c.CompraId == compraId);
        }

        if (compra != null) return compra;

        // 2. Si no fue por ID, buscar si referencia coincide con CodigoUnico de Ticket
        var ticket = await _db.Tickets
            .Include(t => t.Compra).ThenInclude(c => c.Evento).ThenInclude(e => e.Venue)
            .Include(t => t.Compra).ThenInclude(c => c.Visitante)
            .Include(t => t.Compra).ThenInclude(c => c.Tickets).ThenInclude(tk => tk.Asistente)
            .Include(t => t.Compra).ThenInclude(c => c.DetalleCompras).ThenInclude(d => d.TipoEntrada)
            .FirstOrDefaultAsync(t => t.CodigoUnico == referencia);

        return ticket?.Compra;
    }
}
