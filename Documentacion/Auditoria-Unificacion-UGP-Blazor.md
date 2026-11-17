# Auditoría de unificación UGP / Blazor

Fecha: 12 de noviembre de 2026. Auditoría estática del árbol local, sin cambios de código ni consultas SQL.

## Resumen ejecutivo

La aplicación ASP.NET Core ya reúne navegación, portada, cartelera, detalle, directorio, contacto, FAQ, checkout y boletos. La unificación **es parcial**: no equivale a una réplica visual completa ni a una plataforma comercial lista. El checkout permanece bloqueado por seguridad y la integración auténtica Wompi sigue pendiente según los informes disponibles.

Hallazgo prioritario: Home.razor aún inserta peleadores, patrocinadores y testimonios de demostración cuando SQL devuelve vacío o falla. Los testimonios se presentan con «Asistente Verificado». Eso contradice una presentación pública basada únicamente en datos reales. El directorio /peleadores, en cambio, consulta SQL y maneja vacío/error. La siguiente entrega recomendada es **consistencia del contenido público y estados vacío/error**, sin tocar pagos ni esquema.

No se asigna porcentaje de avance: la existencia de un archivo o una compilación no acredita comportamiento ni paridad visual.

## 1. Fuentes y alcance

Backend: `C:\Users\Yurgen Velasco\source\repos\UGP(ultimo guerrero en pie)\UGP(ultimo guerrero en pie)`.

Fuente original consultada, solo lectura: `C:\Users\Yurgen Velasco\source\repos\ugpmma.com.co\ugpmma.com.co`. Está disponible. Contiene **dos representaciones distintas**: exportaciones HTML/WordPress y una adaptación Express/EJS. No deben confundirse: la plantilla index.ejs es más reducida que index.html. No se certificó que la copia exterior sea idéntica; no se modificó ninguna copia.

Se inspeccionaron rutas, plantillas, componentes, consultas, modelos, recursos y documentos previos. No se arrancaron las aplicaciones, no hubo navegación de navegador ni capturas comparativas a 360/768/1440. La comparación visual siguiente es de estructura y estilos, no una aprobación visual renderizada. No se consultó SQL ni se inspeccionaron secretos. La existencia de entidades no demuestra que haya contenido real poblado.

## 2. Inventario original

| Grupo | Archivos / evidencia | Observación |
|---|---|---|
| Entrada Node | package.json, server.js, app.js | Express ^4.18.2 y EJS ^3.1.9; scripts start/dev arrancan server.js, puerto 3000 predeterminado |
| Rutas conectadas | routes/index.js | GET /, /contacto, /preguntas-frecuentes, /peleadores, /avisame-ugp, /carrito-2 |
| Plantillas | views/pages/index.ejs, contacto.ejs, preguntas-frecuentes.ejs, peleadores.ejs, avisame-ugp.ejs, carrito-2.ejs | Varias son resúmenes estáticos de las exportaciones |
| Componentes | views/partials/head.ejs, header.ejs, footer.ejs, scripts.ejs; views/layouts/main.ejs | Archivos compartidos existentes; no asumir que todas las páginas los componen correctamente sin renderizar |
| Producto y 404 | controllers/productoController.js, views/pages/producto/detalle.ejs, views/pages/404.ejs | El router montado no conecta productoController. app.js devuelve su propio HTML mínimo 404; plantilla 404 no prueba ruta activa |
| Exportación | index.html; contacto/index.html; peleadores/index.html; preguntas-frecuentes/index.html; producto/lda38/index.html; producto/velada-de-traders/index.html; carrito-2 y avisame-ugp | Referencia adicional de diseño, no backend funcional |
| Estilos y medios | public/css/estilos.css, estilos.css por página, wp-content/uploads, CSS Kadence y plugins | Recursos heredados y duplicados entre exportación/adaptación |
| JavaScript | public/js/scripts.js, wp-includes y plugins | Carrusel y configuración heredada de GTranslate, WooCommerce y tracking |
| Formularios | views/pages/avisame-ugp.ejs | POST /avisame-ugp con nombre/apellido y campos de captura; el router solo contiene GET. No hay evidencia de recepción funcional en Express |
| Comercio externo | scripts.js, wp-content/plugins | URLs wp-admin/admin-ajax.php, wc-ajax, WooCommerce, FooEvents/seating y Bold; archivos descargados no equivalen a servicios PHP operativos |
| Idiomas | public/js/scripts.js | Configuración GTranslate en/es; dependencia externa, no traducciones propias acreditadas |
| Administración | Recursos WordPress exportados | Sin panel administrativo Node funcional identificado; no confundir enlaces wp-admin con implementación local |

app.js sirve public y wp-content/wp-includes, no convierte por sí solo las exportaciones HTML en rutas comerciales ni ejecuta PHP. La versión EJS /peleadores contiene fichas genéricas «Peleador UGP/LDA» con estadísticas fijas: no es un catálogo de atletas verificado.

## 3. Inventario Blazor

| Grupo | Implementación encontrada |
|---|---|
| Raíz | Program.cs, Components/App.razor, Routes.razor; Router de una sola assembly y MainLayout predeterminado |
| Layout | MainLayout/NavMenu y CSS aislado; menú móvil, footer, WhatsApp, ReconnectModal |
| Rutas públicas | /; /eventos/{EventoId:int}; /peleadores; /contacto; /preguntas-frecuentes; /not-found; /Error |
| Compra y comprobante | /checkout y /checkout/{EventoId:int}; /checkout/confirmacion y /checkout/confirmacion/{Referencia} |
| Controladores | PasarelaController, WompiEventosController, CompraAccessController, BoletosController |
| Servicios | CompraService, CompraAccess, TicketService, TicketPdfService, QrService; WompiCheckout, WompiWebhookPolicy, ReservasService/Worker, ConciliacionPolicy, ReservaTransaccion |
| Datos públicos | ApplicationDbContext y VentaEventoPublica; páginas Home/EventoDetalle/Peleadores consultan EF directamente |
| Modelos | Evento, EventoTipoEntrada, TipoEntrada, Peleador, Combate, HistorialCombate, Patrocinador/Patrocinio, Experiencia y entidades multimedia, ContenidoSitio, WhatsApp; compras, reservas, asistentes y tickets |
| Formularios | CheckoutFormVm y validación; consulta de referencia en Confirmacion; verificador QR completo en Home; búsqueda local FAQ |
| Recursos | wwwroot/css/ugp-theme.css, public-pages.css, CSS aislado; images/ugp/logo.png, portada-escritorio.jpg, portada-movil.jpg |
| Scripts | wwwroot/js/compra-access.js, wompi-checkout.js, Blazor y Bootstrap; módulo de reconexión |
| Dependencias visuales | Bootstrap local, Bootstrap Icons CDN, Google Fonts Inter/Oswald/Barlow Condensed; imágenes Unsplash en respaldos de Home |
| Administración | Identity configurado y entidades existentes, pero no páginas administrativas ni CRUD administrativo encontrados en el inventario de rutas |

Hay entidades Venue/VenueZone, WhatsApp, contenido e IA, sin que ello acredite UI o servicios operativos para esos módulos. Hay migración histórica versionada; el arranque actual no la ejecuta. No se propone activarla.

## 4. Matriz de cobertura funcional

Estados: **implementado en código** no implica probado en esta auditoría; **parcial**, **pendiente**, **bloqueado** y **sin evidencia** se usan expresamente.

| Módulo | Frontend original | Blazor actual | Estado | Pendiente |
|---|---|---|---|---|
| Inicio | index.ejs y exportación index.html, distinta extensión de contenido | Home con datos EF, estadísticas, secciones y QR | Parcial | Retirar respaldo ficticio; cerrar paridad visual |
| Carruseles | Cuatro campañas estáticas, tarjetas, indicadores, cuenta regresiva/avísame | Hasta cuatro eventos con precio disponible, controles y CSS de movimiento reducido | Implementado en código | Pruebas de teclado, foco, 0/1/4 eventos y capturas |
| Cartelera | Ancla proximos-eventos y ofertas exportadas | #eventos, consulta real y helper común de venta | Parcial | Todos los eventos se ordenan por fecha sin filtro de futuros; revisar título «Próximos» y tratamiento del histórico |
| Detalle eventos | Exportaciones producto/lda38 y velada; plantilla producto no conectada al router | /eventos/{id}, EF, combates, localidades y CTA | Implementado en código | Validación real visual/404/error/disponibilidad sin habilitar compra |
| Localidades | Recursos FooEvents/seating y producto | TipoEntrada/EventoTipoEntrada, precios SQL y cupos calculados | Parcial | No es silletería numerada; no afirmar mapa de asientos funcional |
| Checkout | Carrito, dependencias WooCommerce/Bold; Express no demuestra procesar pagos | Flujo propio Blazor y Wompi, validación servidor | Bloqueado por seguridad | Secreto auténtico/eventos y pruebas Sandbox; no migrar carrito WooCommerce |
| Tickets/QR | Recursos FooEvents; sin emisión local demostrada | Servicios QR/PDF y acceso por compra; consulta pública de QR completo | Implementado con evidencia previa aislada/SQL | Regresión HTTP de accesos y prueba real de pago separadas |
| Combates/peleadores | Fichas EJS genéricas; diseño exportado | Directorio EF y detalle con Combate.EventoId, rojo/azul/ganador; destacados Home | Parcial | Home aún inventa atletas en vacío/error; no hay perfiles individuales ni clasificación LDA/UGP acreditada |
| Patrocinadores | No se acreditó módulo operativo en router/plantillas inspeccionados | Home consulta Patrocinadores y rellena marcas ficticias | Parcial | Solo datos publicados verificables, vacíos y aprobación de logos |
| Quiénes Somos | No ruta ni módulo conectado acreditado en Express | No página independiente encontrada | Pendiente de definición | Texto/medios aprobados, propósito y ruta; no inventar historia corporativa |
| Experiencias / galería | index.html contiene ugp-exp y galerías escritorio/móvil; index.ejs no conserva toda esa composición | Home muestra reseñas Experiencia; existen modelos multimedia | Parcial y divergente | No confundir testimonios con galería; conectar recursos reales aprobados |
| Contacto/WhatsApp | Plantilla de canales y enlaces exportados | /contacto con dos números por finalidad, correo/redes; footer/flotante coinciden en entradas | Implementado en código, oficialidad sin verificar | Confirmación del propietario y revisión editorial; no hay formulario de envío |
| Idiomas | GTranslate en/es en scripts heredados | html lang=es; sin recursos .resx/localización encontrados | Pendiente si se aprueba alcance | Traducción editorial y formato, sin copiar tracking/traductor automáticamente |
| Administración | Sin backend admin local acreditado | Identity y modelos; no pantallas admin encontradas | Pendiente | Requisitos, autorización y CRUD independiente; no exponer por inferencia de modelos |
| Historial eventos | Ofertas estáticas históricas; no archivo dinámico acreditado | Consulta Home incluye todos los eventos, sin página histórica dedicada | Parcial | Separar futuros/pasados y resultados con reglas explícitas |
| Responsive | CSS propio/exportado y variantes de galería móvil | Media queries, menú móvil, portada móvil y desktop | Implementado en CSS, sin evidencia visual actual | 360/768/1440, zoom, scroll horizontal, teclado, foco y cargas |
| FAQ | Plantilla original | Búsqueda local, details/summary, contenido limitado y políticas pendientes señaladas | Implementado en código | Revisión editorial y teclado en navegador |
| Avísame | GET y formulario POST sin handler conectado | No página ni servicio | Pendiente opcional | Consentimiento, retención, antiabuso y canal de envío antes de implementar |
| 404 | Fallback inline Express; plantilla separada sin conectar | Router NotFoundPage y componente que fija 404 si puede | Implementado en código | HTTP real, navegación interactiva y enlaces desde páginas anidadas |

## 5. Comparación visual y evidencia concreta

Se conservan logo, paleta oscura/dorada, portada desktop/móvil y navegación pública. La estructura original del hero incluye campañas con títulos/editorial, promociones y avísame; Blazor sustituye esas ofertas por hasta cuatro eventos SQL habilitados. Esa sustitución es correcta para no vender campañas ficticias, pero no es identidad de contenido ni composición pixel a pixel.

La exportación tiene galerías ugp-exp__galeria y versiones móviles (index.html, líneas aproximadas 394–608); Home usa tarjetas de testimonios, no esa experiencia multimedia. Las únicas imágenes incorporadas bajo images/ugp son logo y dos portadas. Los medios del modelo no están automáticamente integrados por existir sus entidades.

App.razor declara Inter/Oswald/Barlow Condensed, Bootstrap Icons CDN y Bootstrap local. El original incorpora CSS Kadence/plugins y hojas propias. No se verificó igualdad de métricas tipográficas ni de fuentes efectivamente cargadas. Home.razor.css incluye breakpoints 991/767 y prefers-reduced-motion; esto no prueba ausencia de desbordamiento.

**No se realizaron capturas ni pruebas de navegador en esta auditoría.** No se certifican fidelidad, rendimiento, Console/Network, enlaces externos ni responsive por revisar CSS. La siguiente entrega debe aportar capturas antes/después en las tres anchuras solicitadas.

## 6. Arquitectura y hallazgos

1. **Un frontend principal en el código ASP.NET:** Routes/App/MainLayout componen Blazor; no se encontró runtime Express incorporado ni package.json Node en el inventario del backend. No se inspeccionaron procesos/hosting para afirmar que solo exista un frontend desplegado en toda la máquina.
2. **Rutas:** no se encontró duplicación exacta entre directivas @page. Checkout y Confirmacion tienen variantes int/referencia; no son por sí mismas duplicados. Los controladores complementan pagos y recursos protegidos. Falta recorrer rutas en HTTP para descartar conflictos efectivos.
3. **Reutilización:** compra/tickets/QR permanecen en servicios; Home, Peleadores y EventoDetalle consultan DbContext directamente. VentaEventoPublica comparte criterio de venta entre inicio y detalle. No hay necesidad demostrada de rehacer arquitectura.
4. **Datos ficticios públicos, confirmado:** Home.razor:376–420 conserva CargarDatosDemostracionSiVacio. Añade atletas con estadísticas, marcas patrocinadoras y reseñas inventadas; la sección de reseñas etiqueta «Asistente Verificado». No debe considerarse contenido real por no estar en el checkout. Un único catch de consultas informativas favorece que un error parcial deje secciones con respaldo ficticio.
5. **Calendario, confirmado:** Home:338 ordena todos los eventos sin filtro de fecha; VentaEventoPublica.Habilitada solo exige saldo y estado Activo/Próximo. No basta el título «Próximos» para garantizar actualidad. Cambiar elegibilidad de compra requiere revisar negocio en otra entrega; esta auditoría no la modifica.
6. **Secciones dispares:** /peleadores maneja datos SQL, vacío/error, mientras Home presenta demostraciones. Experiencias expone verificación sin evidencia asociada en el render.
7. **Herencia no integrada:** el original incluye controllers/modelos de producto no montados, EJS y HTML duplicados, assets PHP y JS dependientes de WP. No migrar esas dependencias como si fueran lógica funcional.
8. **Dependencias actuales:** fuentes/CDN e imágenes remotas del respaldo; no se observaron llamadas WooCommerce/WordPress en el layout y scripts propios integrados. El respaldo de imágenes remotas y el título HTML «Ultimate Grappling Promotion» merecen revisión de identidad frente a «Último Guerrero en Pie».
9. **Disponibilidad y bloqueo:** se muestran enlaces a checkout por disponibilidad del evento, independientemente del mantenimiento de pagos. Es una diferencia entre oferta visual y disponibilidad del servicio, no una razón para retirar la guardia del servidor. Señalización de mantenimiento debe diseñarse sin activar compras.
10. **Administración:** RouteView no sustituye protección de operaciones administrativas futuras. Tener Identity y entidades no acredita un dashboard ni sus permisos.

## 7. Estado de entregas y bloqueos

Entrega 1 (layout) y 1B (portada): implementadas en código, paridad visual no acreditada hoy; contenido de respaldo impide declararlas completas como datos públicos reales. Entrega 2: rutas públicas encontradas, pruebas visuales actuales pendientes y canales requieren aprobación editorial. Entrega 3: detalle y relación Combate/Eventos comprobables; consultas reales y renders no ejecutados hoy.

Seguridad/validación/compra/QR: implementaciones presentes. Los informes de SQL/HTTP/worker documentan pruebas anteriores con límites expresos, no equivalen a una nueva regresión funcional del sitio. Wompi auténtico sigue bloqueado por secreto de eventos y prueba con comercio, según Validacion-Wompi-Autentica-Sandbox-UGP.md. Deadlocks y COMMIT incierto siguen pendientes. Esta auditoría no revalida configuración privada ni habilita flags.

## 8. Plan priorizado de finalización

| Entrega | Objetivo y archivos | Dependencias / riesgos | Pruebas y aceptación |
|---|---|---|---|
| V1 — contenido público confiable (siguiente) | Home.razor y CSS mínimo; coherencia con Peleadores | Aprobación de ocultar placeholders; no cambiar compra/modelo | SQL vacío/error/datos reales por sección; no atletas/marcas/reseñas inventados ni «verificado» sin evidencia; preservar QR/carrusel/cartelera; 360/768/1440 y teclado |
| V2 — cierre visual | Home/layout/NavMenu/CSS/medios indispensables | Referencia visual elegida: exportación HTML frente a EJS; derechos de medios | Capturas comparativas, enlaces reales, sin scroll horizontal, foco visible, movimiento reducido; checkout/confirmación sin regresión visual |
| V3 — calendario e histórico público | Home, EventoDetalle, criterio de fechas a definir | Regla de zona horaria/estados y fecha aprobada; no alterar inventario tácitamente | Futuros/pasados/cancelados, enlaces de detalle, vacío/error; título refleja conjunto real; CTA respeta guardia servidor |
| V4 — galería/experiencias reales | Modelos multimedia existentes, Home y componente pequeño | Contenido y permisos aprobados; confirmar relaciones SQL sin migrar automáticamente | Imágenes/video aprobados, carga diferida, alt/subtítulos cuando corresponda, vacío/error; sin testimonios ficticios |
| V5 — contenido institucional y contacto | Contacto, FAQ, futura página institucional si se aprueba | Responsable confirma canales, textos y políticas | Sin promesas de reembolso/recuperación/sillas inexistentes; enlaces oficiales y navegación completa |
| V6 — opcionales posteriores | Idiomas, Avísame, perfiles individuales | Alcance explícito, privacidad/retención/canal externo | Pruebas propias antes de publicar; no incorporar WP ni formularios sin receptor |
| S1 — cierre Sandbox (separado de lo visual) | Wompi/host aislado y documentación existente | Secreto auténtico, comercio común, endpoint HTTPS limitado | Evento real firmado, aprobada/rechazada/pendiente cuando reproducible, conciliación/reinicio; ninguna habilitación pública automática |
| S2 — resiliencia y publicación | Harness SQL, reservas, despliegue/Git | SQL desechable y aprobación de despliegue | Deadlocks, reintentos, commit incierto, regresión de acceso PDF/QR, respaldo y rollback; revisión de secretos y commits explícitos |
| A1 — administración futura | Módulos nuevos solo tras requisitos | Autorización/roles, datos reales y contratos definidos | Acceso indebido rechazado, CRUD validado y trazabilidad; no crear dashboard como simple copia visual |

Las líneas V y S son independientes salvo pruebas de regresión: terminar diseño no autoriza cobros; validar pagos no justifica publicar información inventada. Silletería/productos/carrito permanecen fuera de esta finalización mínima hasta diseño aprobado.

## 9. Estado Git y acciones realizadas

Backend: main, un commit por delante de origin/main según referencia local, con numerosos cambios rastreados y archivos sin seguimiento de entregas anteriores (componentes, servicios, SQL, Tests y documentación). No se preparó índice, hizo commit/push ni descartó trabajo.

La lectura de archivos del original fue posible, pero `git -C ... status/rev-parse` devolvió Permission denied en este entorno. No se confirma aquí el hash 5f7d8b9 indicado históricamente ni limpieza de su árbol. No se modificó el original para resolverlo.

Único archivo creado por esta auditoría: este informe. No se compilaron ni ejecutaron aplicaciones, scripts SQL o migraciones. No se modificaron bases, credenciales ni código. Las referencias de línea son orientativas del árbol actual, no de HEAD, porque el trabajo pendiente forma parte del alcance.

## Recomendación final

Autorizar primero **V1: eliminar la sustitución pública de datos reales por demostraciones y distinguir vacío/error por sección**. Es acotada, independiente de Wompi y corrige una inconsistencia comprobada que afecta la credibilidad del sitio. Mantener pagos bloqueados y solicitar aprobación separada para las demás entregas.
