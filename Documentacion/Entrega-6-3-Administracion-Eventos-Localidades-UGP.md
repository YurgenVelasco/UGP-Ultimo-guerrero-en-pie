# Entrega 6.3 — Eventos y localidades compartidas

Fecha: 2026-11-14. Rama `main`, HEAD `62bdf1d`. Sin commit ni push.

## Dictamen

IMPLEMENTADA PARA INTEGRACIÓN, CON OBSERVACIONES. Compilación y pruebas de servicio/SQL y creación HTTPS satisfactorias. No aprobada para producción ni para la base principal. Pendientes la revisión visual responsive, el recorrido completo de edición/asociación por navegador y la concurrencia administrativa simultánea con reservas reales de prueba.

Precio y capacidad por evento: BLOQUEADOS POR EL MODELO, conforme a la decisión aprobada. No se añadieron columnas ni migraciones.

## Modelo reutilizado y límites

- `Evento`: datos editoriales, estado, aforo y saldo existente.
- `TipoEntrada`: catálogo compartido con nombre global único, precio y capacidad globales.
- `EventoTipoEntrada`: asociación única evento/tipo, sin precio, capacidad ni activación propios.
- `DetalleCompra`: compromisos pendientes/aprobados y precio histórico; `Ticket`: entradas activas/usadas.
- La consulta administrativa reutiliza `VentaEventoPublica.LocalidadesDisponibles`. El saldo disponible se limita además por `Evento.TicketsDisponibles`, sin escribirlo desde un formulario.
- Una localidad vinculada a más de un evento rechaza cambios de nombre, precio y capacidad. No se simulan tarifas independientes. No hay eliminación, desasociación ni desactivación.
- Se permite crear tipos y editar tipos exclusivos o sin asociaciones cuando no tienen referencias históricas incompatibles. El precio de detalles/tickets ya existentes nunca se actualiza.

## Funcionalidad y rutas

| Ruta privada | Función |
|---|---|
| `/administracion/eventos` | Listado, hasta 200 eventos |
| `/administracion/eventos/nuevo` | Creación de evento |
| `/administracion/eventos/{EventoId:int}` | Consulta, edición editorial, estado, disponibilidad y asociación de localidad existente |
| `/administracion/localidades` | Catálogo global hasta 500 tipos y creación |
| `/administracion/localidades/{TipoId:int}` | Consulta y edición restringida del tipo global |

Dashboard enlaza estas rutas; no se agregaron enlaces a la navegación pública. Home, detalle público, checkout, Wompi, QR y PDF no se modificaron.

## Reglas y seguridad

1. Política `AdministracionUGP` en componentes SSR y en cada método público del servicio mediante `AdminOperations.DemandAsync`. Identity, rol SQL y MFA existentes se conservan.
2. Escrituras limitadas en servidor a Development, Windows integrada, `(localdb)\MSSQLLocalDB`, base exacta `UGP_Integracion_Test`. Cualquier otro destino se rechaza. No basta cambiar una ruta o habilitar un botón.
3. Formularios SSR `EditForm`, antifalsificación, validación de anotaciones y reglas del servicio. Nombre/lugar se recortan; espacios, límites, precios con más de dos decimales e imágenes fuera de la política editorial se rechazan. No se aceptan imágenes/HTML arbitrarios.
4. Aforo se fija al crear; no se puede alterar en un evento existente. Inicialización del saldo una sola vez, sin descuentos al asociar tipos.
5. Capacidad de un tipo exclusivo no baja de pendientes + máximo(ventas aprobadas, tickets activos/usados). Se rechaza precio nuevo mientras haya compras pendientes. Suma de capacidades asociadas no supera el aforo.
6. Finalizar/cancelar con compras pendientes se rechaza; reapertura de estados cerrados requiere revisión independiente.
7. Transacciones serializables y estrategia de reintento EF existente. Bloqueo administrativo `UGP:AdminCatalog` y bloqueos de inventario existentes por evento en orden ascendente. No se modificó el mecanismo de reservas/pago.
8. Versiones de formulario protegidas con Data Protection rechazan edición obsoleta. Nombre global se verifica bajo bloqueo y conserva la restricción única SQL. Asociación repetida es idempotente.
9. Logs `AdminAudit` registran actor, acción e identificador, intención y commit. No son un historial inmutable en SQL; la retención duradera de logs sigue siendo una tarea de despliegue. No se registran credenciales ni contenido de formularios.
10. Errores técnicos se convierten en un mensaje público genérico; validaciones de negocio conservan mensajes comprensibles y los datos del formulario.

## Archivos de esta entrega

Nuevos:

- `Models/ViewModels/AdminCatalogModels.cs`
- `Services/Administration/AdminCatalogService.cs`
- `Components/Pages/Admin/_Imports.razor`
- `Components/Pages/Admin/CatalogPageBase.cs`
- `Components/Pages/Admin/Events.razor`
- `Components/Pages/Admin/EventEditor.razor`
- `Components/Pages/Admin/Localities.razor`
- `Tests/Reservas/AdminCatalogChecks.cs`
- `Tests/Reservas/AdminCatalogHttps.cs`
- Este informe.

Archivos preexistentes modificados conservando el trabajo anterior:

- `Program.cs`: registro del servicio administrativo.
- `Services/Administration/AdminAccess.cs`: texto de estado del panel.
- `Components/Pages/Admin/Dashboard.razor`: enlaces privados; atributos heredados de `_Imports`.
- `Tests/Reservas/Program.cs`: modos de prueba específicos.
- `Tests/Reservas/AdminIntegrationLogin.cs`: opción para recorrido de catálogo con contraseña/TOTP introducidos únicamente en consola local.

## Pruebas ejecutadas

### SQL Server real, servicio directo

Destino verificado antes de escribir: LocalDB y base aislada exacta. Autorización sustituida exclusivamente en el ejecutable de pruebas; estos casos NO prueban por sí solos Identity/MFA ni HTTP.

16 comprobaciones PASA:

- Crear dos eventos y editar/consultar uno.
- Rechazar versión obsoleta y repetición de creación.
- Asociar el mismo tipo a dos eventos; repetición sin tercera asociación.
- Rechazar cambio de precio compartido; ambos precios siguen iguales.
- Rechazar nombre global duplicado.
- Rechazar suma de capacidades superior al aforo, sin asociación parcial.
- No descontar inventario desde administración.
- Rechazar capacidad inferior a cinco compromisos pendientes.
- Rechazar cambio de precio con compra pendiente.
- Disponibilidad muestra cinco reservados/cinco disponibles.
- Guardia rechaza operación no autorizada y no persiste el evento.

Evidencia conservada: eventos 20/21, tipos 18/19/20 y compra sintética 16. La fixture pendiente se insertó explícitamente en la base aislada, sin pago ni emisión de tickets, con saldo coherente de 15 sobre aforo 20. No representa una reserva comercial ni una aprobación. Ejecuciones preliminares dejaron eventos 16–19 y tipos 16/17; no se eliminaron registros.

Primer intento detectó un fallo real en la conversión regional de `Range(decimal)`. Se corrigió con `ParseLimitsInInvariantCulture=true` y se repitió satisfactoriamente. No se cambió la cultura global.

### HTTPS real + Identity SQL + MFA auténtico

Host exclusivamente `https://localhost:7197`, Development, rol configurado, MFA obligatorio, reservas y receptor Wompi deshabilitados. Responsable introdujo contraseña y TOTP en consola oculta. No se almacenaron esos valores ni cookies.

16 comprobaciones PASA, evidencia local ignorada por Git: `artifacts/admin63-https-results.txt`:

- Dashboard/API anónimos HTTP 401.
- Login con contraseña y MFA.
- Listado de eventos, alta y catálogo HTTP 200.
- POST de evento sin CSRF HTTP 400.
- Alta por formulario Blazor y redirección correcta.
- Detalle administrativo conserva el nombre creado.
- Detalle público muestra el mismo evento, HTTP 200.
- Dashboard/API autenticados HTTP 200.
- Cookie segura, logout sin CSRF rechazado, logout válido HTTP 302 y acceso posterior HTTP 401.

Además, cuatro GET anónimos a listado, alta, catálogo y detalle administrativo respondieron HTTP 401.

### Regresión aislada

- 30 comprobaciones de reservas/Wompi sintético: PASA; no son integración auténtica Wompi.
- 24 comprobaciones de galería: PASA.
- 29 comprobaciones HTTPS/Identity en memoria: PASA, incluyendo usuario autenticado sin rol HTTP 403 y MFA obligatorio fuera de Development. No equivalen a probar el nuevo formulario con un usuario SQL sin rol.
- Compilación del proyecto y ejecutable de pruebas: cero errores y advertencias.
- `git diff --check`: PASA; Git advierte normalización LF/CRLF en archivos preexistentes, sin errores de whitespace.

### Pendientes explícitos

- Revisión visual real a 360, 768 y 1440 px; teclado, foco y lector de pantalla. Se usaron columnas Bootstrap, labels y mensajes de campo, pero eso no sustituye una prueba visual.
- POST HTTPS de edición de evento, alta/edición de tipo y asociación (las operaciones sí se probaron mediante servicio y SQL).
- Usuario SQL autenticado sin rol contra las rutas nuevas; rechazo de guardia y política probados aisladamente, no este escenario completo.
- Dos solicitudes administrativas simultáneas y una reserva concurrente. Bloqueos y versiones implementados; edición obsoleta probada, simultaneidad real pendiente.
- Fallo SQL al COMMIT con resultado incierto. No afirmar idempotencia completa ante ese escenario.
- Precios/capacidades distintos por evento requerirían otra decisión y esquema autorizado; no implementados.

## Git y cierre

`main`, HEAD `62bdf1d`, adelantada un commit respecto a la referencia local `origin/main`. Se conservan 24 archivos versionados modificados y múltiples archivos/directorios sin seguimiento de entregas anteriores. El listado agrupado de Git no cuenta individualmente los archivos dentro de directorios nuevos.

No se ejecutaron add, commit, push, reset, clean, migraciones ni scripts SQL. No se tocaron secretos, roles ni esquema. No se modificó la base principal. Las únicas escrituras de datos fueron fixtures y operaciones administrativas autorizadas en integración.

Host HTTPS detenido al finalizar. Configuración de pruebas aplicada sólo al proceso; sin cambios persistentes de flags. Binarios y evidencia bajo `artifacts/` permanecen ignorados por Git. Índice sin cambios preparados.

Antes de una integración definitiva: completar pendientes HTTP/visuales y concurrencia, revisar por separado el diff de cada entrega y aprobar una selección explícita de archivos. No usar `git add .` sobre el árbol acumulado.

## Entrega 6.3.1 — Inicio de validación, 2026-11-14

- Árbol Git inspeccionado y conservado, sin commits ni push.
- Compilación actual de aplicación y pruebas: PASA, cero errores/advertencias (`artifacts/admin631-checks`).
- Host HTTPS aislado iniciado en localhost:7197, LocalDB UGP_Integracion_Test, MFA obligatorio, reservas y webhook deshabilitados. Detenido al interrumpirse la automatización.
- Navegador integrado: fallo al crear sesión (pestaña fuera de sesión y timeout Statsig).
- Alternativa Brave: Computer Use detuvo el turno porque no pudo determinar la URL actual con suficiente confianza para aplicar su política. No se eludió esa protección.
- Recorrido de navegador, responsive, concurrencia y recuperación de conexión de esta etapa: NO EJECUTADOS. No se reutilizan resultados previos como evidencia nueva.
- Sin escrituras de datos de prueba en esta etapa, sin cambios de credenciales/esquema/código funcional.
- Dictamen 6.3.1: VALIDACIÓN INCOMPLETA por bloqueo de la herramienta de navegador. Requiere reanudar las comprobaciones; no aprobada por compilación solamente.

## Recuperación de Entrega 6.3.1 — 2026-11-14 (Colombia)

Esta sección actualiza el intento bloqueado por Computer Use. No se utilizó automatización de navegador; HTTP y SQL son pruebas independientes. La revisión visual sigue pendiente.

### Cambios de pruebas

- Nuevo `Tests/Reservas/AdminConcurrencyChecks.cs`, modo `--admin-concurrency` en Program de pruebas.
- Ampliado `AdminCatalogHttps.cs`: creación, edición, localidad, asociación, validación, SQL y consulta pública mediante HTTP real.
- Ampliado `AdminIntegrationLogin.cs`: rechazo de rutas CRUD sin rol usando token CSRF válido; resultados fallidos producen código de salida distinto de cero.
- Sin cambios de código funcional, esquema, credenciales, checkout ni Wompi.

### Concurrencia y recuperación: 10 comprobaciones PASA

Evidencia: `artifacts/admin631-concurrency.txt`. Evento sintético 23, tipo 21; sin compras ni tickets. Dos DbContext y conexiones independientes, salida simultánea mediante barrera y dos identidades de prueba. Autorización sustituida sólo en este ejecutable: no representa dos sesiones MFA, que se verifican aparte en HTTP.

| Escenario | Evidencia SQL y resultado |
|---|---|
| Dos ediciones del mismo evento | Un commit y una ValidationException por versión obsoleta; SQL coincide exactamente con el ganador |
| Dos cambios de estado | Un commit y un conflicto explícito; no hay pérdida silenciosa |
| Dos asociaciones simultáneas | Ambas llamadas completan; SQL conserva exactamente una relación |
| Timeout después de SaveChanges, antes de COMMIT | Excepción sintética en interceptor; estrategia EF hace dos intentos, transacción fallida se revierte, cambio final correcto |
| Desconexión real | Se cierra únicamente una conexión propia Pooling=false durante WAITFOR, con actualización no confirmada; comando interrumpido y rollback verificado |
| Recuperación | Nueva operación por AdminCatalogService persiste correctamente después de la desconexión |
| Invariantes | Saldo 20, sin compras/tickets, una asociación |

No se ejecutaron KILL, cambios globales ni interrupción del servicio SQL. El timeout inyectado y la desconexión real son escenarios distintos. No se probó COMMIT incierto ni un deadlock real; no se afirma garantía universal ante esos fallos.

### HTTPS y seguridad

Host: `https://localhost:7197`; verificado que escucha sólo `127.0.0.1` y `::1`. Development, LocalDB UGP_Integracion_Test, rol configurado y MFA obligatorio. Reservas=false, webhook=false. Contraseña/TOTP introducidos por responsable únicamente en consola oculta.

Primer recorrido: creación, edición, reflejo público, localidad, asociación idempotente, SQL, login/MFA y logout pasaron. Tres expectativas del test fallaron por esperar 401/403 en POST sin CSRF; se confirmó por solicitudes independientes HTTP 400. La protección antifalsificación actuó antes de la autorización. Se corrigió exclusivamente el test para enviar token válido y comprobar 401/403 por separado. Evidencia inicial conservada en `artifacts/admin631-https-first-results.txt`.

La prueba sin rol retira temporalmente la asignación de la cuenta de integración mediante Identity y la restaura en finally; no crea roles/cuentas ni modifica contraseña/TOTP. Es una sesión autenticada real que pierde privilegios, no un usuario ficticio presentado como autenticación real.

### Revisión visual manual preparada (NO EJECUTADA)

URL verificadas:

- Login: https://localhost:7197/acceso-ugp — HTTP 200 anónimo.
- Dashboard: https://localhost:7197/administracion — HTTP 401 anónimo.
- Eventos: https://localhost:7197/administracion/eventos — HTTP 401 anónimo.
- Crear evento: https://localhost:7197/administracion/eventos/nuevo.
- Localidades: https://localhost:7197/administracion/localidades — HTTP 401 anónimo.

Procedimiento para el responsable:

1. Abrir el login en el navegador local. Introducir usuario autorizado, contraseña y código vigente de Google Authenticator. No compartirlos ni capturarlos en evidencia.
2. Desde el dashboard abrir Eventos y Crear evento. Nombre distintivo QA-VISUAL, lugar sintético, fecha futura, estado Programado y aforo 20. Guardar y comprobar redirección al detalle.
3. Editar nombre/lugar, guardar y visitar Ver detalle público. Confirmar los mismos textos. No intentar comprar.
4. En el catálogo crear una localidad QA-VISUAL con precio 10000 y capacidad 10. En detalle de evento seleccionarla y asociarla. Repetir asociación y verificar que sólo aparece una vez.
5. Crear otro evento sintético con aforo 20 y asociar la misma localidad. Intentar cambiar el precio/capacidad global: debe mostrarse el bloqueo por uso compartido y conservar ambos eventos.
6. Probar nombre vacío y duplicado, capacidad inválida; confirmar mensaje legible y datos conservados. No editar registros que no sean sintéticos de esta prueba.
7. En herramientas de desarrollo del navegador, activar emulación responsive a 360, 768 y 1440 píxeles CSS, zoom 100%. Revisar dashboard, listado, formulario evento y catálogo. Registrar ancho, página, captura sin datos privados y PASA/FALLA.
8. Comprobar ausencia de desbordamiento horizontal, etiquetas visibles, botones alcanzables, mensajes sin recorte, orden Tab lógico, foco visible y activación por teclado. Revisar Console/Network sin exportar cookies/tokens.
9. Cerrar sesión y abrir directamente listado/detalle administrativo; acceso debe denegarse. No considerar la revisión visual aprobada hasta recibir evidencia de estas páginas en navegador real.

El host se mantiene local para esta revisión solicitada. No se publica a Internet ni se persisten sus overrides de configuración. Detenerlo al terminar la revisión; no usar la base principal.

### Compilación, regresión y Git

Compilación de aplicación y pruebas: cero errores y advertencias. Regresión: 30 comprobaciones aisladas de reservas/webhooks, 24 de galería y 29 HTTPS/Identity en memoria aprobadas. No se confunden con pruebas auténticas Wompi. `git diff --check` sin errores.

Rama main, HEAD 62bdf1d, 24 archivos versionados modificados y 50 entradas sin seguimiento agrupadas, incluyendo cambios anteriores. Sin add/commit/push, reset ni limpieza. Sólo archivos de pruebas y este informe modificados en la recuperación.

### Cierre de recuperación 6.3.1

Segunda ejecución HTTPS final: **34 comprobaciones PASA, cero FALLA**, leídas del archivo `artifacts/admin63-https-results.txt` después de la confirmación del responsable. Se probaron contraseña y MFA con Identity SQL, alta y edición de evento por POST Blazor, consulta pública con los cambios, alta de localidad, asociación y repetición sin duplicados, nombre vacío rechazado, SQL conservado e inventario/tickets intactos.

Con token CSRF válido: anónimo GET/POST HTTP 401; sesión real sin rol SQL GET de eventos/localidades y POST de modificación HTTP 403. Sin CSRF: HTTP 400. Rol restaurado mediante Identity y comprobado por la prueba. Logout HTTP 302 y acceso posterior HTTP 401. Los tres fallos iniciales correspondían a expectativas incorrectas del test, no a acceso indebido. No se alteró la lógica del panel para hacer pasar las pruebas.

**Dictamen actualizado:** validación automatizada HTTPS/SQL y concurrencia APROBADA para los escenarios ejecutados en integración. Revisión visual/responsive MANUAL PENDIENTE. No equivale a aprobación de producción ni de Wompi. Persisten los límites documentados sobre deadlocks reales, COMMIT incierto y concurrencia con operaciones comerciales. La prueba de recuperación automática usa timeout inyectado; la desconexión real demuestra rollback y recuperación posterior, no COMMIT incierto.

Host local permanece disponible para la revisión manual solicitada, MFA obligatorio y checkout/webhook deshabilitados. No se crearon módulos nuevos. Los únicos cambios de esta recuperación son pruebas y documentación. Sin commits ni push.

## Entrega 6.3.2 — Pruebas reales de navegador (2026-11-15)

### Dictamen

**CRUD y controles examinados: APROBADOS en integración. Presentación responsive: REQUIERE CORRECCIONES.** Se completó un recorrido real con MFA, creación/edición, asociaciones compartidas, rechazo de modificaciones, reflejo público y logout. No se modificó código funcional para corregir los defectos visuales. No es autorización para producción ni para activar checkout/Wompi.

### Entorno y navegador

- Instancia existente: PID 30108, dotnet ejecutando `artifacts/admin631-recovery/UGP(ultimo_guerrero_en_pie).dll`, proyecto actual. No se inició otro host ni se cambió puerto.
- HTTPS `https://localhost:7197`, listeners 127.0.0.1 y ::1 únicamente. Se conservó el proceso con los overrides previamente establecidos: Development, LocalDB MSSQLLocalDB / UGP_Integracion_Test, Windows integrada, MFA obligatorio, Reservas=false y webhook=false.
- Preflight SQL de sólo lectura: esquema Identity compatible, 1 usuario, 1 rol, 1 asignación y 1 cuenta MFA. La salida Rol configurado=False corresponde al diagnóstico separado; el host tiene su propio override de rol.
- Navegador integrado: abrió login y llegó al dashboard tras intervención del responsable; falló al cambiar viewport con `Tab 4 is not part of browser session ...`, seguido de `Timed out waiting for Statsig values`. No se insistió indefinidamente.
- Alternativa usada: Microsoft Edge con Playwright Core 1.57.0 ya incluido en las herramientas instaladas. Sin instalación ni descarga, perfil/contexto temporal nuevo, `chromiumSandbox:true`, `ignoreHTTPSErrors:false`. No se importaron cookies ajenas. Responsable introdujo credenciales y TOTP directamente; no se capturó el login.
- Al cerrar las pruebas se ejecutó logout real y se cerraron los navegadores temporales de Playwright. El host preexistente se dejó ejecutándose, con sus flags originales.

### Casos funcionales observados

| Caso | Resultado y evidencia |
|---|---|
| Login con MFA y dashboard | PASA: navegación real posterior al login a /administracion y heading visible |
| Listado administrativo | PASA: abierto desde enlace del dashboard |
| Nombre con espacios | PASA: no crea evento; error junto al campo y resumen, datos restantes conservados |
| Crear evento | PASA: eventos sintéticos 26 y 27, prefijo QA632-1794719472986 |
| Editar evento 26 | PASA: lugar Laboratorio visual editado, confirmado posteriormente por SELECT |
| Estado vacío | PASA: antes de asociar se muestra No hay localidades asociadas |
| Detalle público | PASA: nombre del evento real creado visible en /eventos/26 |
| Asociación compartida | PASA: tipo sintético existente 21 asociado a eventos 26 y 27; también conserva su asociación previa con 23 |
| Cambiar precio compartido | PASA: mensaje de bloqueo; SELECT y GET posterior conservan 10000 |
| Cambiar capacidad compartida | PASA: bloqueo; nuevo GET conserva 10 |
| Teclado | PASA parcial: Tab de Nombre global a Precio COP; no se realizó auditoría completa de lector de pantalla |
| Menú móvil | PASA: botón abre menú, aria-expanded=true, captura a 360 px |
| Logout | PASA: botón real Cerrar sesión; siguiente navegación a /administracion/eventos HTTP 401 |

SQL de integración: eventos 23, 26 y 27 conservan saldo 20, cero compras y cero tickets; tipo 21 precio 10000 y capacidad 10. Evento 23 mantiene lugar Recuperado. Se guardó evidencia de campos sintéticos exclusivamente en `artifacts/admin632-visual/sql-evidence.json`. No se modificaron eventos reales ni datos históricos.

### Responsive y hallazgos

| Vista | 360 px | 768 px | 1440 px |
|---|---|---|---|
| Dashboard | Contenido accesible; observar recorte del botón de menú en captura cerrada | Distribución legible | Distribución legible |
| Formulario evento, validación y asociación | Secciones apiladas; mensajes y guardar visibles; revisar margen derecho | Dos columnas, asociación legible | Dos columnas, tarjetas y controles visibles |
| Catálogo localidades | FALLA: scrollWidth=387 para viewport=360; 14 enlaces salen del viewport | Sin overflow global, pero nombres largos exceden tarjetas | Sin overflow global; tres columnas |

Hallazgos sin corregir:

1. **V632-01, media:** nombres globales largos sin espacios desbordan tarjetas del catálogo. Confirmado por captura y medición a 360; también se ven salidas de los límites de tarjeta a 768 aunque el documento no tenga overflow global. Corrección mínima propuesta: permitir quiebre de palabras en enlaces/títulos de las tarjetas y comprobar min-width/anchos flex. No ocultar el problema con overflow global. Archivo candidato: Components/Pages/Admin/Localities.razor y CSS aislado de ese componente.
2. **V632-02, baja:** validación del nombre en inglés (`The Nombre field is required.`). Proponer mensajes españoles en AdminCatalogModels.cs sin cambiar reglas. No aplicado.
3. **V632-03, observación visual:** el menú cerrado y el borde derecho de controles aparecen muy próximos o recortados en capturas de 360. El menú sí abre y se comprobó semántica expandida. Revisar el espacio útil con scrollbar y contenedores antes de atribuir una causa CSS. No se considera aprobada toda la presentación móvil sólo por pasar scrollWidth.

No hay tablas en estas páginas: los listados se representan como tarjetas. No se inventó una prueba de tabla inexistente.

### Console y Network

- Cero eventos `pageerror` JavaScript en recorridos registrados.
- Recorrido principal: cero HTTP >=400 y cero errores Console; 11 requestfailed sobre /_blazor/disconnect durante navegaciones.
- Cierre: único HTTP >=400 fue 401 esperado tras logout; un error Console contabilizado y 5 requestfailed sobre /_blazor/disconnect.
- No se guardaron cuerpos, cookies, tokens, query strings ni mensajes potencialmente sensibles. Sólo host/ruta/código y contadores. Al no conservar detalle de requestfailed no se afirma su causa ni que todos sean inocuos: queda pendiente clasificar las desconexiones y el mensaje Console. No se detectaron fallos HTTP de recursos de imagen/CSS en el registro.

### Evidencias y reproducibilidad

Nuevo archivo de prueba: `Tests/Reservas/AdminVisual632.cjs`. Requiere Playwright instalado en la ruta local documentada y el tipo sintético 21; no es un runner portable ni crea usuarios. Ejecutar únicamente en el entorno aislado autorizado. Modo `--finish` reutiliza registros, sin nuevas altas.

Evidencia ignorada por Git, sin secretos: carpeta `artifacts/admin632-visual/`:

- results.json: recorrido CRUD y primer hallazgo de overflow.
- finish-results.json: catálogo, teclado, menú y logout.
- sql-evidence.json: verificación independiente SQL.
- dashboard-360/768/1440.png.
- validacion-evento-360/768/1440.png.
- evento-asociacion-360/768/1440.png.
- catalogo-final-360/768/1440.png, localidad-bloqueada-360.png y menu-360.png.

Se revisaron visualmente las capturas, no sólo las medidas. Los valores 11000 o 11 en capturas con error son entradas rechazadas conservadas en formulario, no valores persistidos. SQL y GET posterior demuestran que permanecen 10000 y 10.

Se corrigieron exclusivamente defectos del test: selector del título sensible a mayúsculas CSS, comparación textual de decimal y recarga que repetía POST inválido en lugar de un GET nuevo. Se conservó evidencia preliminar; esas fallas del test no se atribuyen al panel. No se modificaron componentes, servicios ni políticas funcionales.

### Verificación final y límites

- dotnet build: cero errores y advertencias, salida artifacts/admin632-build.
- Suite administrativa HTTPS aislada existente: 29 comprobaciones PASA; distinta del recorrido de navegador auténtico.
- git diff --check: sin errores.
- No se repitieron deadlocks, COMMIT incierto ni las pruebas de concurrencia de 6.3.1 en esta etapa visual.
- Pendientes: corregir visuales previa autorización, repetir responsive tras corrección, clasificación detallada de disconnect/Console y accesibilidad completa.
- Rama main, HEAD 62bdf1d, trabajo acumulado conservado. En esta etapa sólo se añadió el script de prueba y se actualizó este informe; capturas bajo artifacts ignorado. Sin commits, push, migraciones, cambios de credenciales, esquema o base principal.

## Entrega 6.3.3 — Recuperación visual y rediseño administrativo (2026-11-16)

### Diagnóstico previo

La presentación genérica no se debía a CSS o JavaScript ausentes. Edge recibió los siete recursos de estilo y los scripts revisados con HTTP 200 y MIME correcto. Se comprobaron Bootstrap, app.css, ugp-theme.css, public-pages.css, el bundle de CSS aislado, ReconnectModal, blazor.web.js y bootstrap.bundle.js. Antes del cambio los inputs tenían fondo blanco y las páginas administrativas usaban el layout público y estilos genéricos; faltaba una composición administrativa específica. El catálogo medía 387 px con viewport de 360 px.

No fue necesario modificar MapStaticAssets, las referencias de App.razor, el middleware, caché ni el transporte Blazor.

### Implementación y archivos de esta entrega

- NUEVO Components/Administration/AdminNavigation.razor: navegación administrativa reutilizable.
- NUEVO Components/Layout/AdminLayout.razor: sidebar de escritorio, menú móvil nativo details/summary, enlace de salto, contenido y logout con antiforgery.
- NUEVO Components/Layout/AdminLayout.razor.css: identidad carbón/dorado, tarjetas, formularios, foco visible, adaptación responsive y quiebre de nombres largos.
- MODIFICADO Components/Pages/Admin/_Imports.razor: selección del layout administrativo; conserva autorización y renderizado existente.
- MODIFICADO Components/Pages/Admin/Dashboard.razor: resumen obtenido de las consultas existentes, con límites de consulta indicados; enlaces y estados de carga/error.
- MODIFICADO Components/Pages/Admin/Events.razor: listado y tarjetas de eventos.
- MODIFICADO Components/Pages/Admin/Localities.razor: catálogo, formulario y avisos de localidades compartidas.
- MODIFICADO Components/Pages/Admin/EventEditor.razor: formulario, datos y asociación de localidades.
- MODIFICADO Models/ViewModels/AdminCatalogModels.cs: mensajes de DataAnnotations en español, sin cambiar límites de negocio.
- NUEVO Tests/Reservas/AdminVisual633.cjs: recorrido real de navegador sin escrituras válidas de catálogo.
- ACTUALIZADO este documento.

No se modificaron entidades EF, servicios de compra/inventario, autenticación, MFA ni políticas. Se reutilizan servicios administrativos y sus controles. No se añadieron paquetes, frameworks, datos ficticios o JavaScript de navegación. Los registros mostrados son los existentes en la base de integración, no contenido de producción.

### Entorno y seguridad

Host aislado https://localhost:7197, Development, Windows integrada, (localdb)\MSSQLLocalDB / UGP_Integracion_Test. Configuración del proceso: rol autorizado configurado, MFA exigido, Reservas:Habilitadas=false y Wompi:Webhooks:Habilitados=false. No se modificaron secretos ni configuración principal. El responsable introdujo credenciales y TOTP directamente en Edge; la automatización no guardó cookies, contraseñas o códigos.

La restricción de no modificar bases se respetó para eventos/localidades: solamente consultas y formularios inválidos. El login utiliza el funcionamiento normal de Identity. No se repitió CRUD válido ni pruebas transaccionales que escriben datos. La evidencia SQL de CRUD de 6.3.2 sigue siendo evidencia histórica, no una nueva validación de esta entrega.

### Pruebas y evidencias

Navegador real Microsoft Edge 157.0.4341.60 mediante Playwright ya instalado, certificado HTTPS validado, sin instalar herramientas ni omitir controles.

| Comprobación | Resultado observado |
|---|---|
| Dashboard, eventos, localidades, edición y creación a 360/768/1440 | PASA: 15 mediciones; scrollWidth coincide con viewport en todos los casos |
| Catálogo móvil con nombres largos | PASA: 360 px frente a 387 px antes; nombres se parten dentro de tarjetas |
| Formulario inválido de evento | PASA: nombre obligatorio en español, otros datos conservados, permanece en formulario |
| Asociación con ID 0 | PASA: mensaje Selecciona una localidad; sin asociación válida |
| Menú móvil | PASA: abre navegación administrativa |
| Teclado | PASA parcial: Tab de nombre global a precio; no auditoría completa de lector de pantalla |
| Logout | PASA: siguiente GET administrativo HTTP 401 |
| Evento inexistente | HTTP 404 y página pública española sin detalles internos; el test textual FALLA porque esperaba Evento no encontrado y el middleware presenta Página no encontrada |
| Recursos CSS/JS posteriores | HTTP 200/304 y tipos correctos; nuevo bundle aislado cargado |
| Excepciones JavaScript pageerror | Cero registradas |

El archivo results.json contiene 24 PASA y 1 FALLA. No se convierte retroactivamente el fallo del selector textual en PASA; la captura after-error-768.png permite revisar la respuesta real. La primera sesión de navegador se cerró antes del recorrido posterior: Target page, context or browser has been closed. Se conserva interrupted.json y se repitió en una ventana nueva con intervención privada del responsable.

Se revisaron capturas de escritorio, tablet y móvil, incluyendo validación de 360 px. Evidencia local ignorada por Git en artifacts/admin633-visual/: before-dashboard-1440.png, before-localidades-360.png, before.json, results.json; after-dashboard, after-eventos, after-localidades, after-editar-evento y after-crear-evento en los tres tamaños; after-validacion-360.png, after-menu-360.png y after-error-768.png. No se incluyen secretos en esas evidencias.

### Clasificación de desconexiones

Las solicitudes fallidas observadas a /_blazor/disconnect tienen net::ERR_ABORTED durante navegación. El JavaScript realmente servido registra pagehide y llama navigator.sendBeacon para desconectar el circuito. El layout público anterior incluye NavMenu InteractiveServer; las páginas administrativas son SSR. El nuevo layout administrativo no introduce ese componente interactivo. En el recorrido posterior quedó una solicitud abortada de disconnect después de visitar la página pública 404. No hay evidencia en este recorrido de un fallo de recursos ni de una respuesta 500 que justifique cambiar el transporte. Esta clasificación aplica a las solicitudes observadas; no afirma que cualquier desconexión futura sea inocua.

El harness registra pageerror y requestfailed, no todos los mensajes de Console: una auditoría exhaustiva de Console queda pendiente y no se declara aprobada.

### Compilación y regresión

- dotnet build --no-restore -p:UseSharedCompilation=false -o artifacts/admin633-after: cero errores y advertencias.
- Compilación de checks en artifacts/admin633-checks: cero errores y advertencias.
- Suite aislada: 30 PASA.
- Suite galería: 24 PASA.
- Suite administrativa HTTPS aislada: 29 PASA; incluye autorización, CSRF, logout y política MFA. No equivale a repetir CRUD real SQL.
- git diff --check: sin errores.

### Riesgos residuales y dictamen

Presentación administrativa y ausencia de overflow verificadas en los tamaños solicitados. Correcciones V632-01 y V632-02 verificadas; menú móvil accesible observado. Pendientes: ajustar y repetir la expectativa textual del test 404, accesibilidad integral, Console exhaustiva y regresión CRUD SQL con escrituras cuando se autorice. No se inyectó fallo SQL ni se vació el catálogo para simular estados. La recuperación MFA y preparación para producción continúan fuera del alcance.

Dictamen: rediseño implementado y comprobado visualmente, listo para revisión con observaciones. No se declara aprobación funcional integral ni preparación para producción. Checkout y Wompi permanecen deshabilitados.

Git: main, HEAD 62bdf1d, ahead 1. Se preservan los 24 archivos versionados modificados y los archivos sin seguimiento acumulados; los componentes administrativos de esta entrega ya pertenecen al conjunto sin seguimiento. No se ejecutó add, commit, push, reset, clean ni migraciones. Las capturas y compilaciones permanecen bajo artifacts ignorado. No se inició otro módulo.
