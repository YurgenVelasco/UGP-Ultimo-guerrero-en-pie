# Entrega 4 — Integridad de Home

## Actualización QA en navegador — 12 de noviembre de 2026

Se abrió realmente Home mediante el navegador integrado contra el host loopback 127.0.0.1:5188, usando la compilación artifacts/home-entrega4 y SQL Server UGP_Integracion_Test. Reservas y webhooks se forzaron a false solo mediante variables del proceso; clave QR sintética para QA. No se cambiaron secretos persistidos ni datos SQL.

Resultados observados:

- PASA: consulta SQL y render de 15 eventos de fixtures existentes (enero de 2099). Son registros sintéticos de la base de pruebas, no eventos comerciales reales ni fallbacks generados por Home.
- PASA: «No hay peleadores publicados por ahora», «No hay experiencias publicadas por ahora» y «No hay patrocinadores publicados por ahora». No aparecieron las tarjetas ficticias retiradas ni Asistente Verificado.
- PASA: botón siguiente cambió el carrusel de evento 1 a evento 2, indicador activo y enlace /eventos/2.
- PASA: QR-QA-INVALIDO recibió «No se pudo verificar el código QR». No se probó un QR auténtico.
- PASA: navegación por enlace del carrusel a /eventos/2; se observaron nombre, fecha, lugar, localidad con precio SQL y vacío de combates.
- Responsive medido con viewport 360, 768 y 1440: scrollWidth respectivamente 345, 753 y 1425, sin desbordamiento horizontal del documento. Se capturaron vistas parciales durante la sesión; no se guardaron artefactos de captura. La medición y las vistas parciales no constituyen una aprobación visual exhaustiva de toda la página ni de todos los enlaces.
- PENDIENTE: eventos pasados y activos ya iniciados, porque el catálogo de pruebas observado contenía fechas futuras y no se alteraron registros para forzar esos casos. Criterios de Colombia conservan las comprobaciones aisladas anteriores.
- PENDIENTE: observación del estado inicial de carga; el primer render observado ya había concluido las consultas.

Se levantó un segundo host loopback 5189 con conexión deliberadamente inaccesible a 127.0.0.1:1. La navegación terminó en ERR_CONNECTION_TIMED_OUT, sin poder observar los mensajes de error de Home. Además, ese host arrancado en aislamiento registró UnauthorizedAccessException de Data Protection. **La prueba SQL inaccesible no está aprobada**: falta repetir sin ese impedimento y medir el efecto de prerenderización/reintentos sobre el tiempo de respuesta. No se atribuye el timeout exclusivamente a SQL ni se afirma que el error se confundiera con catálogo vacío.

No se hicieron cambios correctivos de código en esta revisión. Dictamen actualizado: **validación funcional parcial con pendientes**, no aceptación completa. Se solicitaron paradas de ambos hosts al cerrar; sin commits, push, migraciones ni cambios de base principal. Las pruebas previas de compilación siguen documentadas abajo, no se repitieron en esta revisión de navegador.

Fecha: 12 de noviembre de 2026. Implementación local; no se modificó ninguna base de datos.

## Archivos

- `Components/Pages/Home.razor`.
- Este informe.

Se conservaron los cambios anteriores del árbol Git. No se modificaron servicios de compras, reservas, pagos, QR, detalle, administración ni gobernanza SQL.

## Correcciones

Se retiró CargarDatosDemostracionSiVacio y sus atletas, estadísticas de peleadores, marcas y testimonios inventados. Se eliminaron cifras globales +15K y 100% sin fuente. La fotografía de un atleta sin foto ahora se identifica como no disponible, sin atribuirle una imagen de Unsplash. Peso/nacionalidad/apodo/registro deportivo ausentes ya no se convierten en datos afirmativos inventados. Se conservaron logo y fondos decorativos legítimos de la portada.

Las reseñas solo proceden de Experiencias. Se retiraron «Asistente Verificado», insignia de verificación y la afirmación de opiniones verificadas. Sin descripción se informa su ausencia; sin calificación válida no se generan cinco estrellas por defecto. El contenido presente en SQL no demuestra por sí mismo autenticidad editorial: validar los registros publicados sigue siendo responsabilidad de contenido.

No se encontraron valores simulados de cupos en Home: se conserva el cálculo existente mediante VentaEventoPublica; no se cambió inventario ni su contrato.

## Carga, vacío y error

Eventos, peleadores, patrocinadores y experiencias tienen banderas de carga/error independientes. Cada consulta informativa tiene su propio try/catch/finally: un fallo no sustituye el contenido de otras secciones por demostraciones. Se muestra un mensaje de carga, un mensaje de catálogo vacío solo si la consulta termina sin registros y un mensaje de error distinto si falla.

Los contadores muestran un guion durante carga/error en vez de presentar cero como resultado verificado. Los errores registran sección y tipo de excepción mediante ILogger<Home>, sin copiar cadenas de conexión, consultas o mensajes internos al público. El registro es deliberadamente acotado; no incluye stack trace que pueda arrastrar datos sensibles. Las consultas siguen secuenciales para no usar el mismo DbContext concurrentemente.

La prerenderización puede esperar a OnInitializedAsync antes de enviar el HTML final: la existencia de mensajes de carga no acredita que sean visibles en todos los modos de renderizado.

## Eventos próximos y activos

Se convierte DateTimeOffset.UtcNow a la zona de Colombia mediante `SA Pacific Standard Time`, independiente de la zona configurada en Windows. Se interpreta Fecha/Hora del modelo como horario local colombiano. Próximo significa inicio estrictamente posterior al instante de carga. Se conserva el orden fecha/hora/ID y el máximo de cuatro destacados con disponibilidad real.

Los eventos cuyo inicio ya pasó y cuyo estado sigue siendo Activo se muestran separadamente como **Eventos activos iniciados**, con enlace `/eventos/{id}` y advertencia de que el modelo no tiene hora de finalización. No se afirma que sigan en curso solo por la etiqueta. Un evento pasado con estado Próximo no aparece en Próximos; su ruta de detalle permanece intacta. No se modificaron fechas ni se creó un historial nuevo.

El filtro se aplica en memoria tras la consulta existente de eventos, evitando introducir cambios en la traducción EF o en reglas de venta del detalle. La selección se calcula al cargar Home, no es un reloj que reclasifique tarjetas en un circuito abierto durante horas. Los eventos futuros mantienen la elegibilidad comercial existente; no se habilitó Wompi ni se alteró la protección del servidor.

## Pruebas y resultados

| Caso | Resultado / evidencia |
|---|---|
| Compilación del código actualizado | PASA: dotnet build --no-restore -p:UseSharedCompilation=false -o artifacts/home-entrega4; 0 errores y advertencias |
| Pruebas disponibles previas | 30 aisladas PASA ejecutadas desde artifacts/worker-final; corresponden a reservas/webhooks, no a la UI ni a esta nueva compilación |
| Zona Colombia y cambio de día UTC | PASA aislado PowerShell: 2026-11-12 02:00 UTC equivale a 2026-11-11 21:00 Colombia |
| Futuro, pasado, inicio exacto | PASA comparación aislada de fechas; no constituye prueba renderizada de Home |
| Home con datos SQL reales | NO EJECUTADA en navegador |
| Sin peleadores/patrocinadores/testimonios | Ramas revisadas en código; prueba renderizada PENDIENTE |
| SQL inaccesible | Manejo revisado en código; fallo real del host PENDIENTE |
| Evento activo iniciado | Rama y enlace revisados; prueba de navegador PENDIENTE |
| Navegación al detalle y controles del carrusel | Rutas/handlers conservados; navegación real PENDIENTE |
| Responsive 360/768/1440 | NO EJECUTADO; se conservan grids y CSS existentes, sin afirmar aprobación visual |
| QR | Input, llamada EsQrAutenticoAsync y tratamiento del resultado conservados; no se realizó consulta real |
| git diff --check | PASA después de normalizar el final del archivo |

No se inició aplicación ni se escribieron fixtures en SQL para forzar estados. No se atribuyen pruebas de UI a una compilación. Se requiere validación de navegador para cerrar visualmente esta entrega.

## Riesgos y cierre

Revisar calidad editorial de registros existentes: retirar fallbacks no elimina información ficticia que pudiera estar ya almacenada. La ausencia de hora final impide clasificar con certeza eventos en curso. El título del hero sigue refiriéndose a próximos; los activos iniciados tienen su sección separada.

Rama main, cambios preexistentes preservados; sin add/commit/push, migraciones, cambios de secretos o activación de pagos. Únicos cambios propios enumerados arriba.

**Dictamen: corrección implementada y compilada; aceptación visual y escenarios UI/SQL pendientes.** No iniciar otra entrega ni declarar responsive aprobado sin ejecutar navegador.
