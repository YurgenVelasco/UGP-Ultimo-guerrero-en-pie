# Entrega 6.1 — Auditoría e inventario administrativo

Fecha: 2026-11-13, Colombia. Alcance: árbol local actual, incluidos archivos sin seguimiento. Auditoría estática; no se inició la aplicación, no se ejecutó SQL ni se consultaron credenciales. Las referencias a líneas corresponden a esta revisión y pueden desplazarse con cambios posteriores.

## Resumen ejecutivo

**No existe un panel administrativo funcional localizado en este repositorio.** Hay un modelo de datos amplio, infraestructura Identity y servicios de compra/tickets/reservas, pero no controladores administrativos, páginas de autenticación, formularios CRUD administrativos ni políticas administrativas identificadas. Las páginas públicas sí consumen varias de esas entidades. No confundir un DbSet, una migración histórica o un nombre como MostrarEnAdmin con un módulo administrativo implementado.

La siguiente entrega recomendada es **6.2: acceso administrativo y estructura mínima protegida**, reutilizando Identity, después de confirmar por un procedimiento autorizado qué usuarios y roles reales existen. No crear roles o cuentas automáticamente ni habilitar CRUD antes de probar la autorización del servidor. En paralelo puede planificarse el cierre visual pendiente de la galería; no depende de habilitar pagos.

## 1. Inventario comprobado

### Rutas, controladores y presentación

Se inventariaron los archivos C#, Razor y cshtml del proyecto excluyendo bin, obj, artifacts y Tests como implementación productiva.

- `Controllers/BoletosController.cs`: descargas por ticket, por compra e imagen QR; no es administración.
- `Controllers/CompraAccessController.cs`: POST de canje de autorización por compra; no es inicio de sesión administrativo.
- `Controllers/PasarelaController.cs`: GET de checkout Wompi para una compra.
- `Controllers/WompiEventosController.cs`: POST de eventos Wompi firmado; receptor de pagos, no CRUD.
- Páginas Blazor: `/`, `/eventos/{EventoId:int}`, `/peleadores`, `/contacto`, `/preguntas-frecuentes`, `/checkout`, `/checkout/{EventoId:int}`, `/checkout/confirmacion`, `/checkout/confirmacion/{Referencia}`, `/not-found`, `/Error`.
- No se localizaron rutas `/admin`, login/logout ni componentes de gestión de usuarios. No hay archivos cshtml ni directorios funcionales Views/Areas/Pages encontrados en este árbol.
- `Components/Layout/MainLayout.razor`, `NavMenu.razor` y estilos: navegación pública. `Components/Routes.razor:3` utiliza RouteView, no AuthorizeRouteView.
- Formularios existentes: checkout de comprador/asistentes, búsqueda de comprobante, QR público y búsqueda local FAQ. No se encontraron EditForms CRUD administrativos.
- Multimedia reutilizable: `Components/Multimedia/MultimediaGallery.razor`, `GalleryCarousel.razor`, `MediaCard.razor`, CSS aislado y `MediaUrl.cs`. Lectura pública, no edición/subida de archivos.

### Servicios y persistencia

- `ApplicationDbContext : IdentityDbContext<ApplicationUser>` (`Data/ApplicationDbContext.cs:7`), con mapeo explícito de entidades. Es el acceso a datos actual; no se encontró capa de repositorios administrativos que deba duplicarse.
- Interfaces/servicios existentes: ICompraService/CompraService, ITicketService/TicketService, ITicketPdfService/TicketPdfService, IQrService/QrService.
- Infraestructura adicional: CompraAccess, WompiCheckout, WompiWebhookPolicy, ReservasService, ReservaTransaccion, ReservasWorker y ConciliacionPolicy.
- `Data/VentaEventoPublica.cs`: disponibilidad y elegibilidad pública; reutilizar criterio, no crear cálculo administrativo divergente.
- Las páginas Home, EventoDetalle, Peleadores y MultimediaGallery consultan EF directamente. No existen servicios CRUD de eventos/peleadores/patrocinadores en el inventario.
- `Data/DbInitializer.cs:10` conserva Seed con escrituras y datos iniciales; `Program.cs` explícitamente no lo invoca ni migra al arrancar. No usarlo como herramienta administrativa ni activarlo para llenar catálogo.
- Existen migraciones históricas bajo Data/Migrations. Su presencia no demuestra aplicación ni autoriza nuevas migraciones. La gobernanza SQL y sus scripts son un mecanismo de despliegue, no una interfaz de gestión.

### Modelo disponible

`ApplicationDbContext.cs:15–52` contiene:

- Eventos: Evento, TipoEntrada, EventoTipoEntrada, Venue, VenueZone.
- Deporte: Peleador, Combate, HistorialCombate.
- Comercial: Compra, DetalleCompra, Ticket, Asistente, Visitante, MetodoPago, Descuento.
- Contenido: Patrocinador, Patrocinio, Experiencia, SeccionMultimedia, SeccionExperiencia, ExperienciaMultimediaItem, ContenidoSitio.
- Contacto: ConfiguracionWhatsApp, ContactoWhatsApp.
- IA: AIConfiguracion, AITema, AIPalabraRestringida, AIContextoConfig, AIChatHistorial. Sin interfaz administrativa localizada; fuera del alcance propuesto.
- Reservas/pagos mapeados mediante ReservasMapping: ReservaCompra, IntentoPago, TransaccionPago, NotificacionPago, VerificacionPago, IncidenciaPago.
- Identity: ApplicationUser con NombreCompleto, DocumentoIdentidad, Telefono y FechaRegistro, además de los campos heredados. Roles mediante IdentityRole, no una entidad paralela.

## 2. Matriz funcional

Estado de administración y publicación se distinguen expresamente. «Reutilizable» significa disponible en código, no validado en SQL durante esta auditoría.

| Módulo | Estado administrativo | Evidencia actual / reutilizable | Pendiente y riesgo técnico |
|---|---|---|---|
| Eventos | Pendiente; publicación parcialmente implementada | Evento; Home:375; EventoDetalle:135; VentaEventoPublica | CRUD y validaciones de fechas/estados; evitar editar saldo como un campo libre |
| Localidades y capacidad | Pendiente; lógica comercial existente | TipoEntrada, EventoTipoEntrada, Venue/Zone; CompraService:59–169 | TipoEntrada tiene precio/capacidad compartidos; EventoTipoEntrada solo relaciona IDs. VenueZone tiene otro precio/capacidad/disponibles. No asumir que son el mismo inventario ni vender sillas por existir TieneAsientosNumerados |
| Peleadores | Pendiente; lectura pública implementada | Peleador, /peleadores y Home | Altas/edición, validación de imágenes y estadísticas; no inventar clasificación LDA/UGP |
| Combates | Pendiente; detalle público parcial | Combate.EventoId, esquinas roja/azul, ganador; EventoDetalle:157 | Evitar mismo peleador en ambas esquinas, ganador ajeno y resultados contradictorios con HistorialCombate |
| Patrocinadores | Pendiente; listado público parcial | Patrocinador/Patrocinio; Home:419 | Home consulta patrocinadores globales, no usa aquí vigencia/orden del acuerdo Patrocinio. No afirmar que editar un acuerdo cambiará automáticamente Home |
| Galería | Pendiente CRUD; lectura implementada y validación parcial | Tres entidades multimedia y componentes Entrega 5 | Pruebas reales de medios/teclado pendientes; CarruselNumero no es FK a SeccionMultimedia; no confundir Experiencia (reseña) con recurso multimedia |
| Quiénes Somos | Pendiente | ContenidoSitio es estructura genérica potencialmente reutilizable | No ruta ni contrato de clave/contenido encontrado; definición editorial y revisión del esquema antes de usarlo |
| WhatsApp | Pendiente; presentación estática parcial | ConfiguracionWhatsApp y ContactoWhatsApp | MainLayout:14,44 y Contacto:18,24 contienen enlaces literales. Cambiar las tablas hoy no actualiza esos enlaces; confirmar canales/propósito antes de conectar |
| Usuarios administrativos | Bloqueado para exponer gestión; infraestructura parcial | Identity cookies/Core/Roles, ApplicationUser | Sin login/gestión/políticas/roles concretos acreditados. Existencia de cuentas y asignaciones desconocida sin SQL autorizado |
| Compras y tickets | Gestión pendiente; servicios funcionales reutilizables | CompraAccess, CompraService, TicketService, PDFs y QR protegidos | Pagos bloqueados según contexto. No crear aprobación manual ni reutilizar cookie de compra como permiso administrativo; proteger PII |
| Reportes e historial | Pendiente | HistorialCombate y trazas de pagos/reservas | Sin reportes, exportaciones o historial administrativo genérico encontrados. Trazas financieras no son auditoría de cambios de catálogo |

## 3. Seguridad: evidencia y límites

| ID / prioridad | Hallazgo comprobado | Evidencia | Acción mínima antes de administración |
|---|---|---|---|
| ADM-01 / alta | Base Identity sin acceso administrativo completo | Program:28–57 registra cookies, IdentityCore, Roles, SignInManager; no rutas de cuenta ni uso de UserManager/RoleManager en la aplicación encontrado | Reutilizar Identity, comprobar cuentas/roles autorizados; implementar entrada/salida y guardias; no inventar roles |
| ADM-02 / alta | No hay política/atributos administrativos localizados | Búsqueda de Authorize, IsInRole y políticas; Routes:3 usa RouteView; Program no declara AddAuthorization/UseAuthorization explícitos | Definir y probar autorización tanto HTTP como de operaciones Blazor. No inferir ausencia de servicios implícitos del framework solo por no ver un registro explícito |
| ADM-03 / alta para futura exposición | Métodos internos de tickets no reciben contexto de autorización | TicketService:120 ObtenerTicket devuelve nombre del comprador; :170 UsarTicket modifica estado por ID; :198 RegenerarQr | No conectarlos directamente a botones/rutas administrativas. Guardias servidor, validación de operación, trazabilidad y concurrencia antes de exponerlos. No se identificó una ruta pública que invoque UsarTicket: no se afirma explotación actual |
| ADM-04 / media | Sin auditoría administrativa transversal | Existen NotificacionPago, VerificacionPago, IncidenciaPago; no registro actor/antes/después de cambios de catálogo encontrado | Diseñar registro de acciones, retención y acceso sin PII/secrets en logs. Si exige persistencia nueva, solicitar SQL por separado |
| ADM-05 / media | Restricciones EF no equivalen a validación de formularios administrativos | Entidades de catálogo con campos simples; no ViewModels CRUD administrativos | ViewModels acotados, límites de longitud/número/fechas y validación servidor, sin binding indiscriminado de entidades |
| ADM-06 / media | Política de credenciales básica | Program:47–52: mínimo 6, sin mayúscula ni símbolo obligatorios; RequireConfirmedAccount=false | Revisar política, bloqueo de intentos y segundo factor antes de exposición; no cambiar credenciales en esta etapa |

No hay un endpoint administrativo desprotegido demostrado: actualmente no se encontró tal endpoint. Los bloqueos anteriores son requisitos para integrar administración con seguridad, no prueba de acceso ilícito actual.

### Antifalsificación

`Program.cs:92` activa UseAntiforgery. Esto no certifica por sí solo que todos los POST MVC arbitrarios exijan token. No hay formularios administrativos para probar. `WompiEventosController.cs:13–14` permite anónimo y excluye antiforgery por ser webhook con autenticación criptográfica; no trasladar esa excepción al CRUD.

`CompraAccessController.cs:10–14` canjea token Data Protection ligado al navegador; no tiene ValidateAntiForgeryToken explícito. El control de token y navegador es una protección diferente de Identity. No se declara CSRF explotable sin prueba; futuros formularios y endpoints administrativos deben probar rechazo de solicitudes sin token cuando corresponda y autorización independientemente del token.

### Compradores y operaciones críticas

- `BoletosController.cs:39–65`: descarga de orden exige permiso de compra y estado aprobado; QR/PDF individual limita IDs a órdenes permitidas y estados de ticket. No retirar estas condiciones al agregar administración.
- `CompraService.cs:206–222`: verificar pago/consultar referencia comprueba CompraAccess. La autorización del comprador no concede un rol de operador.
- `TicketService.cs:142–167`: consulta pública QR devuelve autenticidad tras comprobar contenido completo, compra aprobada y firma; no es el método UsarTicket.
- `CompraService.cs:37–38` normaliza y valida CheckoutFormVm; los ViewModels tienen Required, StringLength, Range y validación de asistentes. Son validaciones comerciales reutilizables, no cobertura del catálogo administrativo.
- Evitar exponer comprador, documento, correo, QR o token en listados masivos/exportaciones. Definir campos mínimos y permisos separados por operación con los roles existentes que se confirmen.
- La modificación de capacidad/precio con compras activas necesita reglas explícitas y transacciones del sistema actual. No sobrescribir TicketsDisponibles ni VenueZone.Disponibles para «sincronizarlos».

## 4. Arquitectura de integración recomendada

Una sola aplicación Blazor. Reservar `Components/Pages/Admin/` y un layout administrativo dentro de la misma aplicación **como propuesta**, no como rutas ya implementadas. Reutilizar identidad, entidades, DbContext y servicios de compra/reserva/tickets. No copiar Express/MVC ni crear otro panel fuera de UGP.

Para módulos sin servicio actual, agregar operaciones acotadas de catálogo con validación y autorización del servidor, sin repositorio genérico duplicado. Separar ViewModels de entrada de entidades EF para impedir sobreasignación. Contextos de vida corta por operación y reglas de concurrencia explícitas para edición en circuitos Blazor; no retener entidades durante toda la sesión.

Reutilizar MediaUrl para validación de medios admitidos y componentes públicos para previsualización. Reutilizar consultas de disponibilidad; no reutilizar métodos de generación/emisión como acciones manuales de administración. Los servicios financieros conservan sus condiciones actuales. No habilitar Wompi como consecuencia de publicar el panel.

La existencia de los modelos no demuestra coincidencia de la base desplegada: antes de escribir, un procedimiento autorizado debe verificar esquema/versión y datos reales. No ejecutar inicializadores, Migrate, EnsureCreated ni UGP_0003 como parte de este plan.

## 5. Entregas priorizadas propuestas

Rutas/archivos nuevos son propuestas sujetas a aprobación; no se han creado.

| Entrega y alcance | Archivos/módulos | Dependencias y riesgos | Pruebas y criterio de aceptación |
|---|---|---|---|
| 6.2 Acceso y shell administrativo mínimo, sin CRUD | Program, Routes, futuros componentes Account/Admin y layout; Identity existente | Confirmación autorizada de usuarios/roles reales, sesión y política; no asumir un rol Admin | Anónimo rechazado, usuario sin permiso rechazado, operador permitido; acceso directo HTTP y llamada de operación denegados fuera de permiso; logout y caducidad; antiforgery; sin cambiar cookies de compra |
| 6.3 Consulta administrativa de solo lectura | Listados Admin de eventos/peleadores/multimedia; proyecciones DbContext | 6.2; contratos de datos mínimos, paginación, mensajes de error | SQL aislado: vacío/error, filtros y límites, ninguna escritura y sin PII innecesaria |
| 6.4 Edición multimedia acotada | Futuros MultimediaAdmin, ViewModel y operaciones; MediaUrl y tres entidades existentes | 6.2/6.3, auditoría de acciones y esquema verificado; definir edición de encabezado único y grupos sin FK | Activo/inactivo/orden, URL insegura rechazada, cambios reflejados en Home, concurrencia sin pérdida silenciosa; navegador 360/768/1440 y reproducción con medios legítimos. Sin subida arbitraria de archivos |
| 6.5 Peleadores y combates | Páginas/ViewModels/operaciones de catálogo; Peleador, Combate, HistorialCombate | Validar relaciones, definir autoridad del resultado y restricciones de eliminación | Participantes distintos/existentes, ganador participante, historial consistente, detalle público actualizado; doble envío y edición concurrente |
| 6.6 Eventos y localidades | Evento, TipoEntrada, EventoTipoEntrada; VentaEventoPublica y servicios actuales | Definir precios/capacidades compartidos y efecto de cambios sobre reservas; no confundir VenueZone; es la entrega de mayor riesgo comercial | Último cupo, reservas activas, reducción de aforo rechazada si incompatible, no doble descuento, rollback y permisos; pruebas SQL desechables con aprobación independiente |
| 6.7 Patrocinios y contenido institucional/contacto | Patrocinador/Patrocinio, ContenidoSitio, ConfiguracionWhatsApp/ContactoWhatsApp; consumidores públicos | Canales/textos aprobados; contrato de claves y vigencia, sin HTML arbitrario | Vigencia/orden y enlaces seguros, coherencia de footer/contacto/widget, estados vacíos; ninguna promesa comercial no implementada |
| 6.8 Compras/reportes administrativos de solo lectura | Proyecciones restringidas sobre Compra/Ticket y trazas de pagos; componentes Admin | 6.2, minimización de PII, política de auditoría; validación Wompi independiente | Aislamiento de datos, permisos, paginación, sin aprobación/emisión ni consumo de ticket; exportación solo si se aprueba y protege explícitamente |

Gestión de asignaciones de usuarios/roles, escáner de acceso, modificaciones de esquema y operaciones de reembolso requieren alcance y autorización propios; no se incluyen automáticamente. Evitar una pantalla de roles antes de definir quién puede otorgarlos y cómo se audita.

## 6. Evidencia, pruebas pendientes y Git

Ejecutado: inventario de archivos y rutas, lectura de Program/Routes/controladores/servicios/modelos, búsquedas de autorización/formularios/auditoría y comparación de usos públicos. No se ejecutaron pruebas HTTP/SQL, compilación o escenarios de login: no se modificó código y no existe flujo administrativo localizado para acreditar funcionalidad. No se leyeron valores secretos ni se inspeccionaron tablas de usuarios.

Pendientes indispensables: esquema real y roles/asignaciones; pruebas negativas de autorización y antiforgery; sesión/revocación; acceso a PII; acciones concurrentes; requisitos de auditoría duradera; validación multimedia real ya pendiente de Entrega 5. Las pruebas aisladas de entregas previas no prueban estos controles administrativos.

Estado inicial: rama `main`, HEAD `62bdf1d864aa02de4e5e52442083d74dbb43c494`, ahead 1 respecto de la referencia local origin/main, índice sin cambios preparados. Persisten 23 archivos versionados modificados y numerosos sin seguimiento de entregas previas (incluidos Multimedia, Tests, SQL y documentación). No se atribuyen a esta auditoría.

**Único archivo creado en Entrega 6.1: este informe.** No hubo SQL, cambios funcionales, credenciales, migraciones, commits ni push. Se conserva el árbol pendiente. Dictamen: base de datos/modelos y lógica pública reutilizables; administración funcional todavía pendiente, con seguridad de acceso como primer requisito. Detenerse para aprobación antes de implementar 6.2.
