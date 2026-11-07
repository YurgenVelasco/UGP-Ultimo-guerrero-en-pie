using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UGP_ultimo_guerrero_en_pie_.Data;

namespace UGP_ultimo_guerrero_en_pie_.Controllers;

[Route("pagos")]
public class PasarelaController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;

    public PasarelaController(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpGet("wompi/{compraId:int}")]
    public async Task<IActionResult> RedirigirWompi(int compraId)
    {
        var compra = await _context.Compras
            .Include(c => c.DetalleCompras)
            .Include(c => c.Visitante)
            .FirstOrDefaultAsync(c => c.CompraId == compraId);

        if (compra == null) return NotFound("Compra no encontrada.");

        var publicKey = _config["Wompi:PublicKey"] ?? "pub_test_X0zDA9xoKdePuotSdPUxggaGLxDsq4Yt";
        var integritySecret = _config["Wompi:IntegritySecret"] ?? "test_integrity_SecretKeyWompi2026!";
        var fecha = compra.FechaCompra ?? DateTime.UtcNow;
        var referencia = $"UGP-TX-{fecha:yyyyMMdd}-{compra.CompraId:D6}";
        long montoEnCentavos = (long)(compra.MontoTotal * 100);
        string moneda = _config["Wompi:Currency"] ?? "COP";
        string redirectUrl = $"http://localhost:5145/checkout/confirmacion/{referencia}";

        // Firma SHA-256 oficial
        string rawSignature = $"{referencia}{montoEnCentavos}{moneda}{integritySecret}";
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawSignature));
        string firmaIntegridad = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var correo = compra.Visitante?.Correo ?? string.Empty;
        var nombre = compra.Visitante?.Nombre ?? string.Empty;

        // Renderiza el script oficial de WidgetCheckout que Wompi ejecuta en pantalla completa
        string html = $@"<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='utf-8'/>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'/>
    <title>Conectando con Wompi...</title>
    <script type='text/javascript' src='https://checkout.wompi.co/widget.js'></script>
    <style>
        body {{
            background-color: #0b0b0b;
            color: #d4af37;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
        }}
        .box {{
            text-align: center;
            padding: 30px;
            background: #141414;
            border: 1px solid #d4af37;
            border-radius: 8px;
            box-shadow: 0 0 20px rgba(212, 175, 55, 0.2);
            max-width: 480px;
            width: 90%;
        }}
        h2 {{ margin: 0 0 10px 0; font-size: 1.3rem; letter-spacing: 1px; }}
        p {{ color: #ffffff; margin: 5px 0 20px 0; }}
        .btn-retry {{
            display: inline-block;
            background: #d4af37;
            color: #000;
            padding: 10px 20px;
            text-decoration: none;
            font-weight: bold;
            border-radius: 4px;
            cursor: pointer;
            border: none;
            transition: all 0.2s ease;
            font-size: 0.95rem;
        }}
        .btn-retry:hover {{
            background: #f3c644;
            transform: scale(1.02);
        }}
        .btn-simular {{
            display: inline-block;
            background: transparent;
            color: #d4af37;
            padding: 9px 18px;
            text-decoration: none;
            font-weight: bold;
            border-radius: 4px;
            border: 1px solid #d4af37;
            font-size: 0.9rem;
            transition: all 0.2s ease;
        }}
        .btn-simular:hover {{
            background: rgba(212, 175, 55, 0.15);
            color: #ffffff;
        }}
    </style>
</head>
<body>
    <div class='box'>
        <h2>CONECTANDO CON LA PASARELA WOMPI</h2>
        <p>Total a liquidar: <strong>${compra.MontoTotal:N0} COP</strong></p>
        <button id='btnOpen' class='btn-retry' onclick='abrirCheckout()'>ABRIR PASARELA DE PAGO</button>

        <div id='fallbackBox' style='display:none; margin-top: 25px; padding-top: 20px; border-top: 1px solid rgba(255, 255, 255, 0.12);'>
            <p style='font-size: 0.85rem; color: #a0a0a0; margin-bottom: 12px;'>¿El widget tarda en responder o estás en entorno de pruebas?</p>
            <div style='display: flex; gap: 10px; justify-content: center; flex-wrap: wrap;'>
                <button type='button' class='btn-retry' onclick='abrirCheckout()'>Reintentar Apertura</button>
                <a href='{redirectUrl}' class='btn-simular'>Continuar a Comprobante (Dev Mode)</a>
            </div>
        </div>
    </div>

    <script>
        console.log('Iniciando Wompi con:', {{
            currency: '{moneda}',
            amountInCents: {montoEnCentavos},
            reference: '{referencia}',
            publicKey: '{publicKey}'
        }});

        var checkout;
        try {{
            checkout = new WidgetCheckout({{
                currency: '{moneda}',
                amountInCents: {montoEnCentavos},
                reference: '{referencia}',
                publicKey: '{publicKey}',
                signature: {{ integrity: '{firmaIntegridad}' }},
                redirectUrl: '{redirectUrl}',
                customerData: {{
                    email: '{correo}',
                    fullName: '{nombre}'
                }}
            }});
        }} catch (e) {{
            console.error('Error inicializando WidgetCheckout:', e);
            var fb = document.getElementById('fallbackBox');
            if (fb) fb.style.display = 'block';
        }}

        function abrirCheckout() {{
            if (!checkout) {{
                console.warn('WidgetCheckout no instanciado.');
                var fb = document.getElementById('fallbackBox');
                if (fb) fb.style.display = 'block';
                alert('No se pudo conectar con los servidores de Wompi. Verifique la conexión.');
                return;
            }}
            try {{
                checkout.open(function (result) {{
                    var transaction = result.transaction;
                    if (transaction && transaction.id) {{
                        window.location.href = '{redirectUrl}';
                    }}
                }});
            }} catch (err) {{
                console.error('Error al abrir checkout:', err);
                var fb = document.getElementById('fallbackBox');
                if (fb) fb.style.display = 'block';
            }}
        }}

        window.addEventListener('DOMContentLoaded', function() {{
            setTimeout(abrirCheckout, 500);

            // Fallback visual si el widget tarda más de 3 segundos en abrir o responder
            setTimeout(function() {{
                var fb = document.getElementById('fallbackBox');
                if (fb) fb.style.display = 'block';
            }}, 3000);
        }});
    </script>
</body>
</html>";

        return Content(html, "text/html", Encoding.UTF8);
    }
}
