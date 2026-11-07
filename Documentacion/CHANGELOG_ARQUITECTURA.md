# Bitácora Técnica de Cambios y Arquitectura - UGP (Ultimate Grappling Promotion)

**Versión:** 2.0.0-net10  
**Fecha:** Noviembre 2026  
**Autor:** Desarrollador Senior / Arquitecto de Software (.NET 10, EF Core & Ciberseguridad)

---

## [2026-11-06 13:30:00] - Subsistema de Tickets y Criptoseguridad

### 1.1 Corrección Crítica de Concurrencia
* **Problema Identificado (Legacy):** En la versión anterior (`REPO_UGP`), la generación del identificador de ticket dependía de una consulta síncrona `MAX(TicketId) + 1` en tiempo de aplicación. Bajo cargas simultáneas de compras concurrentes, esta consulta provocaba condiciones de carrera (*race conditions*), produciendo folios duplicados (`CodigoUnico`) y fallas de integridad en la base de datos.
* **Solución Arquitectónica:** Se implementó una secuencia nativa en SQL Server:
  ```sql
  CREATE SEQUENCE [seq_ticket_codigo] START WITH 1000 INCREMENT BY 1 NO CYCLE;
  ```
  El servicio [`TicketService`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Services/Implementations/TicketService.cs) consume esta secuencia de forma atómica a través de `SELECT NEXT VALUE FOR seq_ticket_codigo;`, garantizando la unicidad absoluta de los folios en alta concurrencia.

### 1.2 Validación Criptográfica contra Falsificación
* **Firma Digital HMAC-SHA256:** Cada boleto genera un token hash criptográfico firmado con clave secreta (`TicketSecurity:SecretKey`) e emisor (`TicketSecurity:Issuer`) configurados en [`appsettings.json`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/appsettings.json).
* **Payload Seguro del QR:** Formato estructurado `UGP|{CodigoUnico}|{EventoId}|{Folio}|{TokenHash}`.
* **Protección contra Timing Attacks:** En [`QrService.cs`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Services/Implementations/QrService.cs), la comparación de hashes se realiza mediante `CryptographicOperations.FixedTimeEquals`, evitando ataques de canal lateral por análisis de tiempo de respuesta.

### 1.3 Generación de PDFs con QuestPDF
* **Motor QuestPDF (Community License):** Implementado en [`TicketPdfService.cs`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Services/Implementations/TicketPdfService.cs).
* **Formatos Soportados:**
  * **Ticket Individual (Formato A6):** Optimizado para visualización móvil e impresión, con branding oficial UGP (dorado `#D4AF37`, negro profundo), código QR nítido de alta resolución, datos del titular/asistente y términos de seguridad.
  * **Resumen de Compra (Formato A4):** Comprobante integral con desglose de orden, comprador, evento, lista detallada de boletos asociados, totales y políticas de acceso.

---

## [2026-11-06 14:15:00] - Capa de Datos y Migración Inicial

### 2.1 Migración EF Core
* Se generó la migración inicial [`InitialCreate`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Data/Migrations/20261106182136_InitialCreate.cs) cubriendo:
  * Tablas de Identity Core (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, etc.) usando `ApplicationUser`.
  * Modelos de eventos, combates, peleadores, recintos (`Venue`, `VenueZone`), compras, tickets y asistentes (relación 1:1 estricta).
  * Módulos de patrocinio, contenido multimedia y configuración de IA / WhatsApp.
  * Secuencia nativa `seq_ticket_codigo` e índice único en `CodigoUnico`.

### 2.2 Script SQL Idempotente para Auditoría
* Se exportó el script SQL completo en [`Sql/UGP_Database_Schema.sql`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Sql/UGP_Database_Schema.sql) con soporte para transacciones y validaciones de existencia idempotentes (`__EFMigrationsHistory`).

### 2.3 Factoría de Tiempo de Diseño
* Se implementó [`ApplicationDbContextFactory.cs`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Data/ApplicationDbContextFactory.cs) desacoplando la ejecución de herramientas CLI (`dotnet ef`) del ciclo de vida del servidor web.

---

## [2026-11-06 14:35:00] - Inyección de Dependencias en Program.cs
Se registraron en el contenedor IoC:
```csharp
builder.Services.AddScoped<IQrService, QrService>();
builder.Services.AddScoped<ITicketPdfService, TicketPdfService>();
builder.Services.AddScoped<ITicketService, TicketService>();
```
Junto con el soporte de autenticación e Identity Core con cookies para `ApplicationUser`.

---

## [2026-11-06 14:45:00] - Frontend e Interfaz Pública Oficial (ugpmma.com)

### 4.1 Identidad Visual y Hoja de Estilos de Combate
* **Archivo:** [`wwwroot/css/ugp-theme.css`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/wwwroot/css/ugp-theme.css)
* **Paleta de Colores de Combate:**
  * Fondo primario oscuro: `#0B0B0B`, `#0D0D0D`, `#141414`, `#161616`.
  * Dorado de campeonato: `#D4AF37`, `#F3C644`, con resplandores (*gold glow*).
  * Rojo de combate / esquina roja: `#C8102E`.
  * Textos puros y atenuados: `#FFFFFF`, `#A0A0A0`.
* **Tipografías Deportivas:** Integración de Google Fonts `Oswald` (encabezados en mayúsculas condensadas de alta contundencia) e `Inter` (cuerpo de lectura de alta legibilidad).
* **Componentes Visuales:** Clases reutilizables para tarjetas de peleadores (`.fighter-card`), tarjetas de evento (`.event-card`), botones de campeonato (`.btn-ugp-gold`, `.btn-ugp-outline`, `.btn-ugp-red`) e isotipo oficial en formato vectorial SVG.

### 4.2 Arquitectura del Layout (`MainLayout.razor` & `NavMenu.razor`)
* **Navbar Flotante:** Barra superior fija (*sticky*) con fondo oscuro semitransparente (`rgba(11, 11, 11, 0.88)`), desenfoque de fondo (*backdrop-filter: blur(14px)*) y logotipo vectorial de UGP.
* **Navegación Unificada:** Enlaces ancla dinámicos a `Inicio`, `Eventos`, `Peleadores`, `Experiencias`, `Patrocinadores`.
* **Acciones de Cabecera:**
  * Botón prioritario **COMPRAR ENTRADAS** con acento dorado.
  * Acceso directo **Validar Boleto** para validación en puerta por personal del evento.
  * Botón de inicio de sesión / acceso.
* **Footer Profesional:** Estructura en 4 columnas con reseña de la promoción, mapa del sitio, políticas de admisión/privacidad, enlaces a redes sociales oficiales y soporte directo por WhatsApp.
* **Botón Flotante de WhatsApp:** Acceso persistente en esquina inferior derecha para soporte al fanático.

### 4.3 Página Principal Interactiva (`Home.razor`)
* Conexión asíncrona a `ApplicationDbContext` mediante `AsNoTracking()` con respaldo de datos de exhibición dinámicos:
  1. **Hero Section:** Titular impactante del evento estelar, badge de transmisión en vivo, fecha, hora, recinto (`Venue`), póster oficial de campeonato y métricas de la liga.
  2. **Cartelera de Próximos Eventos:** Cuadrícula responsiva de eventos con fecha, recinto, precio de entrada y botón de reserva.
  3. **Roster de Guerreros:** Tarjetas deportivas con fotografía en escala dramática, división de peso, récord oficial de victorias/derrotas y apodo de combate.
  4. **Widget Interactivo de Validación de Boletos:** Formulario en vivo donde se puede ingresar un código (`UGP-2026-XXXXXX`) o escanear un QR para verificar en tiempo real la firma criptográfica y el estado del boleto mediante `ITicketService`.
  5. **Muro de Experiencias:** Reseñas y calificaciones de 5 estrellas de asistentes verificados.
  6. **Muro de Patrocinadores:** Alianzas estratégicas en escala de grises con iluminación y escala dinámica al interactuar.

---

## [2026-11-06 15:00:00] - Estado de Calidad y Compilación
* **SDK:** .NET 10.0.403
* **dotnet build:** `0 Advertencia(s)`, `0 Errores`
* **Tiempo transcurrido:** ~1.7s

---

## [2026-11-06 15:35:00] - Subsistema de Compra, Checkout Reactivo y Pasarela Wompi

### 6.1 Reemplazo Arquitectónico de jQuery por Blazor Server Reactivo
* **Contexto Legacy:** En el repositorio previo (`REPO_UGP`), el proceso de checkout dependía de scripts imperativos en jQuery y JavaScript manipulando el DOM directamente, con validaciones frágiles en el cliente y riesgo de desfase entre los datos del comprador y los boletos emitidos.
* **Modernización en .NET 10:** Se implementó un asistente de compra por pasos (*stepper wizard*) completamente reactivo en [`Components/Pages/Checkout.razor`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Components/Pages/Checkout.razor) utilizando `@rendermode InteractiveServer`. Todo el estado vive en memoria y se sincroniza en tiempo real mediante SignalR con enlace bidireccional (`@bind-Value`).

### 6.2 Modelos de Vista Fuertemente Tipados (`CheckoutViewModels.cs`)
Ubicados en [`Models/ViewModels/CheckoutViewModels.cs`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Models/ViewModels/CheckoutViewModels.cs):
* **`AsistenteInputVm`:** Representa los datos individuales de cada portador de boleto (`NombreCompleto`, `DocumentoIdentidad`, `CorreoElectronico`, `Telefono`) con anotaciones de validación estrictas (`[Required]`, `[EmailAddress]`).
* **`CheckoutFormVm`:** Contenedor de la orden completa: identificadores de `EventoId` y `TipoEntradaId`, `Cantidad` (restringida a un rango seguro de 1 a 10 boletos), información del pagador/comprador y la colección sincronizada de asistentes (`List<AsistenteInputVm>`).
* **`WompiCheckoutVm`:** Parámetros seguros calculados por el backend para invocar el widget o redirect de Wompi (`PublicKey`, `Reference`, `AmountInCents`, `Currency`, `SignatureIntegrity`, `RedirectUrl`, `CustomerEmail`, `CustomerFullName`).

### 6.3 Servicio de Compras Seguro (`ICompraService` y `CompraService`)
Implementado en [`Services/Implementations/CompraService.cs`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Services/Implementations/CompraService.cs):
* **Control Estricto contra Sobreventa (Anti-Race Condition):**
  * La creación de la compra se ejecuta dentro de una transacción con nivel de aislamiento `IsolationLevel.Serializable`.
  * Se verifica atómicamente el aforo restante (`CantidadDisponible >= modelo.Cantidad`) en `EventoTipoEntrada`.
  * Se descuenta el stock de manera inmediata para reservar las plazas mientras se procesa la transacción.
* **Registro de Transacción:**
  * Se persiste la entidad `Compra` con estado `EstadoCompra.Pendiente`, junto con sus registros correspondientes en `DetalleCompra`.
  * Generación de una referencia única de pago estructurada: `UGP-TX-{yyyyMMdd}-{CompraId}`.
* **Cálculo Criptográfico de Integridad Wompi:**
  * Implementación de la firma requerida por la API de Wompi:
    $$\text{Signature} = \text{SHA256}(\text{Reference} + \text{AmountInCents} + \text{Currency} + \text{IntegritySecret})$$
  * Codificación en formato hexadecimal en minúsculas mediante `SHA256.HashData(Encoding.UTF8.GetBytes(cadenaConcatenada))`.
* **Confirmación de Pago y Emisión Automática (`ConfirmarPagoAsync`):**
  * Al recibir la confirmación de la pasarela, actualiza el estado de la compra a `EstadoCompra.Aprobado`.
  * Invoca a `ITicketService.GenerarTicketsParaCompraAsync(compraId)` para emitir los boletos criptográficos y códigos QR definitivos.
  * Si el pago es rechazado o cancelado, se restituye automáticamente el stock a `EventoTipoEntrada`.

### 6.4 Experiencia de Usuario Deportiva (Stepper Interactivo)
1. **Paso 1 - Selección de Localidad y Cantidad:**
   * Tarjetas interactivas con iluminación dorada que exhiben nombre, descripción, precio unitario y aforo restante en tiempo real.
   * Selector dinámico de cantidad (1 a 10 boletos) que recalcula al instante el subtotal y sincroniza la lista de asistentes requeridos.
2. **Paso 2 - Datos del Comprador y Asistentes:**
   * Formulario con `EditForm` y `DataAnnotationsValidator`.
   * Botón de conveniencia *"Copiar datos del comprador al Asistente 1"* para agilizar la compra personal con un solo clic.
   * Campos independientes para cada asistente con validación en caliente.
3. **Paso 3 - Resumen y Checkout Oficial:**
   * Desglose completo de la orden: evento, recinto, localidad, lista nominativa de asistentes y monto total en COP.
   * Botón principal **PAGAR CON WOMPI** que invoca el widget seguro de la pasarela.
   * Modo de simulación para entornos de desarrollo local que permite validar el flujo completo de emisión de boletos y generación de PDF sin requerir pasarela real.

### 6.5 Configuración en `appsettings.json` y `Program.cs`
* Se configuró la sección oficial de Wompi VPOS en `appsettings.json`:
  ```json
  "Wompi": {
    "CheckoutVposUrl": "https://checkout.wompi.co/l/VPOS_YBhj5S",
    "PublicKey": "pub_test_X0zDA9xoKdePuotSdPUxggaGLxDsq4Yt",
    "EventsSecret": "test_events_SecretKeyWompi2026!",
    "IntegritySecret": "test_integrity_SecretKeyWompi2026!",
    "Currency": "COP",
    "RedirectUrl": "http://localhost:5145/checkout/confirmacion"
  }
  ```
* Se registró el servicio en `Program.cs`:
  ```csharp
  builder.Services.AddScoped<ICompraService, CompraService>();
  ```
* Se enlazaron los botones *"Comprar Entradas"* del Navbar y la página de Inicio hacia la ruta reactiva `/checkout/{EventoId}` con redirección segura al Checkout VPOS oficial.

---

## [2026-11-06 15:55:00] - Resiliencia de Conexión, Migración Automática y Control de Errores Transitorios

### 7.1 Resiliencia de SQL Server (`EnableRetryOnFailure`)
- Se configuró la estrategia de reintentos para fallos transitorios en [`Program.cs`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Program.cs):
  * `maxRetryCount: 5`
  * `maxRetryDelay: TimeSpan.FromSeconds(10)`
- Se adaptó [`CompraService.cs`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Services/Implementations/CompraService.cs) para envolver las transacciones serializables de reserva de entradas dentro de `_db.Database.CreateExecutionStrategy().ExecuteAsync(...)`, cumpliendo con los estándares de EF Core para transacciones de usuario bajo estrategias de reintento transitorio.

### 7.2 Migración y Sembrado Automático en el Arranque
- Se agregó el bloque de inicialización en `Program.cs` ejecutando:
  * `db.Database.Migrate()` para aplicar automáticamente cualquier migración pendiente de EF Core.
  * [`DbInitializer.Seed(db)`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Data/DbInitializer.cs) para sembrar el recinto base, las tres localidades oficiales (`General Arena`, `Preferencial Cage-Side`, `VIP Hexágono + Meet & Greet`), el evento principal `UGP 05` y el método de pago `Wompi`.

### 7.3 Validación Estricta de Correo y Manejo Amigable de Excepciones
- En [`Checkout.razor`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Components/Pages/Checkout.razor):
  * Se implementó el método `EsCorreoValido` combinando `EmailAddressAttribute` y `MailAddress.TryCreate` para validar correos reales tanto del comprador como de cada uno de los asistentes antes de habilitar el paso a la pasarela de pago.
- En [`CompraService.cs`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Services/Implementations/CompraService.cs):
  * Se capturan fallos de conexión o base de datos transformándolos en mensajes claros y legibles para el usuario final sin colapsar el circuito SignalR de Blazor Server.

---

## [2026-11-06 21:18:00] - Corrección de URL Wompi VPOS (Prevención de Error 403 en CloudFront)

### 8.1 Diagnóstico de Bloqueo 403 en CloudFront
* **Causa Raíz:** Las URLs de tipo Link de Cobro VPOS que contienen `/l/` (como `https://checkout.wompi.co/l/VPOS_YBhj5S`) corresponden a endpoints estáticos preconfigurados en la plataforma de Wompi. Al concatenar query parameters dinámicos (`?reference=...&amount-in-cents=...&signature:integrity=...`), las políticas de CloudFront / WAF en Wompi bloqueaban la solicitud respondiendo `403 ERROR - The request could not be satisfied`.

### 8.2 Solución Implementada
* **[`CompraService.cs`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Services/Implementations/CompraService.cs):**
  * Se eliminó la concatenación de query parameters.
  * La propiedad `CheckoutUrl` / `CheckoutVposUrl` devuelve **exclusivamente la URL limpia base**:
    ```csharp
    resultado.CheckoutUrl = _config["Wompi:CheckoutVposUrl"] 
                            ?? "https://checkout.wompi.co/l/VPOS_YBhj5S";
    ```
  * Se preserva el registro de la `Compra` en estado `Pendiente`, el `DetalleCompra`, el decremento atómico de aforo y la referencia interna en SQL Server para control y auditoría.
* **[`Checkout.razor`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Components/Pages/Checkout.razor):**
  * Al presionar *"PAGAR CON WOMPI"*, la aplicación redirige de forma directa a la URL oficial mediante `NavigationManager.NavigateTo(CheckoutWompi.CheckoutUrl, forceLoad: true)` apuntando estrictamente a `https://checkout.wompi.co/l/VPOS_YBhj5S`.

---

## [2026-11-06 22:20:00] - Página de Confirmación, Entrega de Boletos Digitales y Descarga de PDF

### 9.1 Métodos de Consulta y Aprobación en la Capa de Servicios
* **Ubicación:** [`ICompraService.cs`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Services/Interfaces/ICompraService.cs) y [`CompraService.cs`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Services/Implementations/CompraService.cs)
* **`ObtenerCompraPorReferenciaAsync(string referencia)`:**
  * Consulta flexible y desacoplada que resuelve la compra tanto por la referencia estructurada `UGP-TX-yyyyMMdd-XXXXXX`, como por identificador numérico directo o por folio único del ticket (`UGP-2026-XXXXXX`).
  * Carga todas las relaciones requeridas mediante Entity Framework Core: `DetalleCompras.TipoEntrada`, `Evento.Venue`, `Visitante`, `Tickets.Asistente`.
* **`AprobarCompraYGenerarTicketsAsync(int compraId)`:**
  * Transiciona de forma atómica el estado de la compra de `Pendiente` a `Aprobado`.
  * Emite los boletos oficiales de acceso mediante `ITicketService`, asignando folios únicos secuenciales con la secuencia de SQL Server `seq_ticket_codigo`, generando tokens criptográficos HMAC-SHA256 y persistiendo las matrices QR correspondientes.
  * Mapea y enlaza en relación 1:1 los datos de los asistentes registrados durante el checkout interactivo.

### 9.2 Controlador REST de Descarga Segura de Boletos (`BoletosController.cs`)
* **Ubicación:** [`Controllers/BoletosController.cs`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Controllers/BoletosController.cs)
* **Endpoints Implementados:**
  * `GET /api/boletos/descargar/{ticketId:int}`: Valida que el boleto exista y que pertenezca a una compra en estado `Aprobado`. Invoca `ITicketPdfService.GenerarPdfTicketAsync` y devuelve el documento en formato A6 como archivo descargable MIME `application/pdf` con nombre `Boleto-{codigoUnico}.pdf`.
  * `GET /api/boletos/descargar-orden/{compraId:int}`: Genera y devuelve el PDF consolidado en formato A4 con el resumen completo de la orden, desglose de entradas, titular y advertencias de seguridad (`UGP-Orden-{compraId:D6}.pdf`).
* **Habilitación en `Program.cs`:** Se registraron `builder.Services.AddControllers()` y `app.MapControllers()`.

### 9.3 Página Interactiva de Confirmación (`Confirmacion.razor`)
* **Ubicación:** [`Components/Pages/Confirmacion.razor`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Components/Pages/Confirmacion.razor)
* **Rutas Admitidas:** `@page "/checkout/confirmacion"` y `@page "/checkout/confirmacion/{Referencia}"` con enlace a parámetros por query string (`?reference=...` y `?id=...`).
* **Experiencia de Usuario Deportiva:**
  * **Estado Pendiente:** Exhibe un panel informativo con el estado de la orden y un botón de simulación/verificación bancaria inmediata para entornos de desarrollo.
  * **Estado Aprobado:** Despliega el isotipo de campeonato con resplandor dorado, tarjeta de resumen de orden y la cuadrícula responsiva de boletos emitidos.
  * Cada tarjeta de boleto muestra en tiempo real el folio oficial, datos del asistente, localidad, imagen nítida del código QR y botón de descarga directa del boleto en PDF individual (A6).
  * Botón de acción global `.btn-ugp-gold` para descargar el consolidado A4 de todas las entradas de la orden con un solo clic.
  * Acceso directo agregado a la barra de navegación superior en `NavMenu.razor` bajo la etiqueta destacada *"Mis Boletos"*.

---

## [2026-11-06 22:36:00] - Migración a Wompi Checkout Web Dinámico (/p/) y Sincronización de Monto Exacto

### 10.1 Sustitución del Link Estático VPOS por Checkout Web Oficial
* **Problema Identificado:** Los links de cobro estáticos VPOS (`/l/`) muestran $0 por defecto o impiden parametrización dinámica desde el comercio sin interacción manual del comprador.
* **Solución Arquitectónica:** Migración al endpoint oficial de Checkout Web Dinámico de Wompi:
  ```json
  "Wompi": {
    "CheckoutUrl": "https://checkout.wompi.co/p/",
    "PublicKey": "pub_test_X0zDA9xoKdePuotSdPUxggaGLxDsq4Yt",
    "EventsSecret": "test_events_SecretKeyWompi2026!",
    "IntegritySecret": "test_integrity_SecretKeyWompi2026!",
    "Currency": "COP",
    "RedirectUrl": "http://localhost:5145/checkout/confirmacion"
  }
  ```

### 10.2 Generación de Parámetros Dinámicos y Criptoseguridad en `CompraService.cs`
* Al crear la compra pendiente, se calcula de forma exacta el monto en centavos:
  $$MontoEnCentavos = MontoTotal \times 100$$
* Se genera la firma criptográfica de integridad SHA-256 obligatoria por Wompi:
  $$FirmaIntegridad = \text{SHA256}(Reference + AmountInCents + Currency + IntegritySecret)$$
* Se construye la URL oficial enriquecida para el Checkout Web `/p/`:
  * `public-key`: Clave pública del comercio.
  * `currency`: `COP`.
  * `amount-in-cents`: Monto exacto calculado por la orden de UGP.
  * `reference`: Identificador único `UGP-TX-yyyyMMdd-XXXXXX`.
  * `signature:integrity`: Firma SHA-256 calculada con `IntegritySecret`.
  * `redirect-url`: Redirección automática de retorno al finalizar el pago hacia `http://localhost:5145/checkout/confirmacion`.
  * `customer-data`: Correo, nombre completo y teléfono del comprador para prellenar la pasarela.

### 10.3 Flujo de Redirección Automática en `Checkout.razor`
* Al presionar *"PAGAR CON WOMPI"*, la aplicación ejecuta `NavigationManager.NavigateTo(CheckoutWompi.CheckoutUrl, forceLoad: true)`, abriendo directamente la pasarela dinámica con el valor exacto pre-cargado.
* Al concluir la transacción bancaria, Wompi redirige de vuelta a `/checkout/confirmacion`, donde se recupera la orden y se presentan las entradas oficiales con códigos QR y descarga de PDF.

---

## [2026-11-06 22:55:00] - Integración Oficial del Widget de Wompi vía JS Interop (Solución Definitiva CloudFront 403)

### 11.1 Eliminación Definitiva de Peticiones GET Parametrizadas
* **Causa Raíz de Seguridad:** Las peticiones GET directas hacia endpoints de Wompi con parámetros query strings son interceptadas y bloqueadas por las reglas del WAF de AWS CloudFront.
* **Solución Arquitectónica Oficial:** Integración del SDK cliente oficial mediante `https://checkout.wompi.co/widget.js`.

### 11.2 Inclusión del Script en `App.razor`
* En [`Components/App.razor`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Components/App.razor), se cargó el script oficial y la función global `window.abrirCheckoutWompi(config)` que instancia `new WidgetCheckout(...)`.
* El modal se despliega directamente sobre la aplicación Blazor y al completarse la transacción redirige de forma transparente al usuario a `config.redirectUrl` (`/checkout/confirmacion`).

### 11.3 Invocación Reactiva en `Checkout.razor`
* En [`Components/Pages/Checkout.razor`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Components/Pages/Checkout.razor), se inyectó `IJSRuntime` y se reemplazó la navegación directa por `await JS.InvokeVoidAsync("abrirCheckoutWompi", ...)`.
* Se preservaron intactas todas las entidades de base de datos, relaciones de dominio y registros existentes.

---

## [2026-11-06 23:05:00] - Selector de Tipo de Documento y Optimización UI/UX de Responsividad

### 12.1 Selector de Tipo de Documento en Comprador y Asistentes
* **Modelos de Vista:** Se integró la propiedad `TipoDocumento` con valor por defecto `"CC"` y validación `[Required]` tanto en [`CheckoutFormVm`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Models/ViewModels/CheckoutViewModels.cs) (comprador) como en [`AsistenteInputVm`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Models/ViewModels/CheckoutViewModels.cs) (asistentes).
* **Interfaz de Checkout (Paso 2):** En [`Checkout.razor`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Components/Pages/Checkout.razor), se implementaron grupos de entrada (`input-group`) estilizados con la estética oscura y dorada de UGP, permitiendo seleccionar entre las opciones oficiales:
  * `CC` - Cédula de Ciudadanía
  * `CE` - Cédula de Extranjería
  * `TI` - Tarjeta de Identidad
  * `PAS` - Pasaporte
  * `NIT` - NIT
* **Copia Rápida:** La función `CopiarDatosCompradorAAsistente1()` sincroniza fielmente `TipoDocumento` hacia el primer boleto.
* **Persistencia del Dominio:** En [`CompraService.cs`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Services/Implementations/CompraService.cs), se asegura que el tipo de documento seleccionado por cada asistente sea transferido al modelo [`AsistenteVm`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Models/ViewModels/CheckoutViewModels.cs) y guardado directamente en la entidad [`Asistente`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Models/Entities/Asistente.cs).

### 12.2 Corrección de Responsividad y Prevención de Desbordamiento de Texto
* **Estilos CSS Globales:** En [`wwwroot/css/ugp-theme.css`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/wwwroot/css/ugp-theme.css), se crearon las clases utilitarias `.order-summary-card` y `.summary-detail-item` con directivas estrictas:
  ```css
  .order-summary-card,
  .summary-detail-item {
      overflow-wrap: anywhere !important;
      word-break: break-word !important;
  }
  ```
* **Resumen de Orden (Paso 3):** Se aplicaron estas clases a la tarjeta de confirmación y a las credenciales dinámicas de Wompi (referencias UGP, hashes criptográficos y correos electrónicos), eliminando cualquier riesgo de desbordamiento horizontal en pantallas móviles o compactas.

---

## [2026-11-06 23:40:00] - Redirección Automática vía Formulario POST Directo a Wompi (Solución Definitiva CloudFront 403)

### 13.1 Causa Raíz Técnica de CloudFront 403
* Las peticiones HTTP `GET` directas con múltiples parámetros en la query string hacia `https://checkout.wompi.co/p/?...` o `https://checkout.wompi.co/l/...` son interceptadas y rechazadas por las reglas WAF de AWS CloudFront.

### 13.2 Controlador de Pasarela y Auto-Apertura Wompi (`PasarelaController.cs`)
* Se implementó [`Controllers/PasarelaController.cs`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Controllers/PasarelaController.cs) con el endpoint `GET /pagos/wompi/{compraId}`.
* El endpoint recupera la compra pendiente con su comprador (`Visitante`), calcula el valor exacto en centavos ($MontoEnCentavos = MontoTotal \times 100$), genera la firma de integridad SHA-256 (`signature:integrity`) y renderiza una vista ligera dedicada en pantalla completa que instancia `new WidgetCheckout(...)` con auto-apertura inmediata y botón de reintento.
* Al ejecutarse en una página HTML independiente sin parámetros en la query string, se evita el bloqueo WAF de CloudFront, Wompi recibe el payload estructurado con exactitud y se previene el despliegue de valores en \$0.

### 13.3 Conexión Reactiva en `Checkout.razor`
* En [`Components/Pages/Checkout.razor`](file:///c:/Users/Yurgen%20Velasco/source/repos/UGP(ultimo%20guerrero%20en%20pie)/UGP(ultimo%20guerrero%20en%20pie)/Components/Pages/Checkout.razor), el botón *"PAGAR CON WOMPI"* ejecuta `RedirigirAWompiConPrecio()`, el cual registra la compra pendiente y redirige inmediatamente a `/pagos/wompi/{compraId}`.







