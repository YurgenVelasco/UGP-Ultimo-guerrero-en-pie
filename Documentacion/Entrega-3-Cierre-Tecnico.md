# UGP — Cierre técnico de la Entrega 3

**Fecha:** 10 de noviembre de 2026

**Dictamen:** requiere correcciones antes de considerar confiable la disponibilidad publicada.

El detalle público cumple el alcance de lectura y navegación. El bloqueo de cierre es preexistente y afecta tanto a la portada como al detalle y al checkout. Una compra que permanece pendiente conserva los cupos aunque haya sido abandonada o Wompi haya devuelto un estado final no aprobado.

## Alcance y evidencia de código

| Archivo | Comprobación |
|---|---|
| [EventoDetalle.razor](../Components/Pages/EventoDetalle.razor) | Declara `/eventos/{EventoId:int}` sin requerir autenticación. Lee evento, localidades y combates con EF Core. La carga de la página no guarda datos ni crea compras. Para un evento inexistente devuelve 404. El enlace de compra se renderiza únicamente si hay localidades disponibles. |
| [EventoDetalle.razor.css](../Components/Pages/EventoDetalle.razor.css) | Encabezado en cuadrícula, banner opcional y presentación a dos columnas desde 992 px cuando hay imagen. |
| [VentaEventoPublica.cs](../Data/VentaEventoPublica.cs) | La venta requiere estado `Activo` o `Próximo` y saldo global mayor que cero. Las localidades requieren precio positivo dentro del límite de importe para una entrada y saldo por localidad. El saldo resta las cantidades de compras pendientes o aprobadas de esa localidad. |
| [Home.razor](../Components/Pages/Home.razor) | El carrusel y la cartelera enlazan al detalle. Usan la misma consulta de localidades y la misma condición de venta. El detalle muestra por localidad el menor valor entre el saldo global y el saldo de esa localidad. Los cupos mostrados por localidad no son sumables como aforo adicional. |

Los combates se leen por `EventoId` con sus relaciones a los peleadores de las esquinas roja y azul. El esquema disponible no registra horario ni orden oficial por combate. El detalle presenta los combates registrados y el estado vacío si el evento carece de ellos.

## Inventario y reservas

| Hallazgo | Evidencia | Evaluación |
|---|---|---|
| **C3-01. Las reservas abandonadas bloquean cupos** | [VentaEventoPublica.cs](../Data/VentaEventoPublica.cs#L20) cuenta toda compra `Pendiente` o `Aprobado` sin aplicar plazo. [Compra.cs](../Models/Entities/Compra.cs) registra `FechaCompra`, sin campo de vencimiento. En el código y esquema versionados revisados no hay proceso que expire una compra pendiente y devuelva su cupo. | **Alta. Confirmado por código.** La condición solicitada no se cumple. Una compra pendiente puede reducir el saldo por tiempo indefinido. |
| **C3-02. El rechazo de Wompi no libera inventario** | [CompraService.cs](../Services/Implementations/CompraService.cs#L271) devuelve un mensaje ante `DECLINED`, `VOIDED` o `ERROR` antes de la transición a aprobado. Esa rama no cambia la compra a un estado terminal ni devuelve inventario. | **Alta. Confirmado por código.** Un estado final no aprobado en la pasarela puede dejar una compra pendiente en UGP. |
| **C3-03. No hay un segundo descuento al confirmar o emitir en el flujo revisado** | [CompraService.cs](../Services/Implementations/CompraService.cs#L123) crea la compra y su detalle, y decrementa `Evento.TicketsDisponibles` en la misma transacción serializable. [CompraService.cs](../Services/Implementations/CompraService.cs#L285) reclama de forma condicional la orden pendiente y emite o activa boletos dentro de la transacción de aprobación. [TicketService.cs](../Services/Implementations/TicketService.cs#L42) guarda boletos sin decrementar el evento. | **Verificado estáticamente.** El saldo global y el saldo por localidad se limitan mediante el mínimo de ambos. La compra se incluye una vez en el saldo por localidad a través de sus detalles y el cambio de pendiente a aprobada conserva esa misma contribución. Falta comprobar reintentos y concurrencia en SQL Server de pruebas. |
| **C3-04. La disponibilidad de la página es una lectura puntual** | El detalle lee primero el evento y luego las localidades. Otra compra puede consumir cupos entre lecturas o después de renderizar. Checkout vuelve a validar dentro de la transacción. | **Observación.** El enlace indica disponibilidad observada. No reserva cupos. |

Una corrección posterior debe resolver el estado y la devolución del saldo global en una sola transacción idempotente. Debe distinguir una reserva abandonada de un pago que aún pueda confirmarse legítimamente y contemplar la conciliación de pagos tardíos. Excluir pendientes antiguos solo en la consulta de portada dejaría descontado el saldo global y produciría resultados incoherentes con checkout.

## Pruebas y seguridad

Compilé el árbol actual en una carpeta temporal con **0 advertencias y 0 errores**. Ejecuté `git diff --check` con código de salida **0**. El arranque de prueba usó Development y `Database:InitializeOnStartup=false`. Las peticiones HTTP fueron anónimas y de lectura.

| Caso | Evidencia observada |
|---|---|
| Inicio y detalle existente | `GET /` devolvió 200 e incluyó enlaces al detalle y al checkout del evento disponible. `GET /eventos/1` devolvió 200, mostró tres localidades y enlazó a `/checkout/1`. |
| ID inexistente o no positivo | `/eventos/2147483647` y `/eventos/0` devolvieron 404 sin CTA de compra. |
| SQL deliberadamente inaccesible | `/eventos/1` devolvió 200 con el aviso público de que el evento no pudo cargarse. El cuerpo no incluyó el CTA ni los indicadores de excepción o traza inspeccionados. La excepción se registra en el servidor. El HTTP 200 de esta rama es una observación para monitoreo. |
| Checkout y confirmación | `/checkout/1` y `/checkout/confirmacion` devolvieron 200. No se realizó una compra. |
| Agotado, combate y responsive | La Entrega 3 registró una prueba aislada de renderizado sin CTA para un evento agotado y con un combate de ejemplo. También registró una prueba en Edge a 360, 768 y 1440 px. Esos escenarios no se repitieron en este cierre. |

### Pruebas pendientes con datos de pruebas

1. Evento real agotado y evento no habilitado con cupos. Comprobar consulta pública, ausencia del CTA y coincidencia con portada.
2. Evento con combates registrados. Comprobar nombres de ambas esquinas y campos opcionales contra los registros de SQL.
3. Medir saldo global, saldo por localidad, detalles y boletos antes y después de reservar, confirmar, repetir la confirmación y emitir. Ejecutar dos verificaciones simultáneas y dos intentos por la última entrada.
4. Después de implementar expiración, verificar abandono, rechazo definitivo, reintentos simultáneos de liberación, reinicio de la aplicación y pago aprobado tardíamente. Comprobar que cada reserva se devuelve como máximo una vez y que ninguna liberación compite incorrectamente con una aprobación.

Estas pruebas requieren SQL Server aislado y, cuando corresponda, Wompi Sandbox. Durante el cierre no se crearon compras, no se emitieron tickets y no se modificaron registros.

## Git y plan de integración

Estado inicial `main` en `62bdf1d`, un commit por delante de la referencia local `origin/main`, con 21 archivos modificados y 16 sin seguimiento. El índice estaba vacío. Este cierre añade únicamente este documento, por lo que el estado esperado es 21 modificados y 17 sin seguimiento.

- **Exclusivos de la Entrega 3:** `Components/Pages/EventoDetalle.razor`, `Components/Pages/EventoDetalle.razor.css` y `Data/VentaEventoPublica.cs`. Este documento es exclusivo del cierre.
- **Compartido:** `Components/Pages/Home.razor`. A la Entrega 3 corresponden los dos enlaces de detalle y la extracción de la consulta de disponibilidad. El archivo conserva los bloques de portada, carrusel y QR de entregas anteriores.
- **Otros archivos sin seguimiento:** páginas de contacto, peleadores y preguntas frecuentes, CSS público y de inicio, logo y portadas, controlador y servicio de autorización de compra, servicio Wompi y sus scripts. Revisarlos en sus grupos antes de incorporarlos.
- **Revisión de candidatos:** la lista de archivos seguidos y no ignorados no mostró binarios, respaldos de SQL, certificados, archivos de usuario de Visual Studio ni archivos de User Secrets según los patrones revisados. El escaneo textual no encontró patrones de claves privadas de Wompi, claves PEM ni contraseñas incrustadas en cadenas de conexión. Es un control acotado del árbol actual, no una certificación del historial. La clave QR histórica continúa pendiente de rotación.

### Secuencia recomendada tras autorización

1. Crear una rama local de recuperación para trabajar con los cambios pendientes. La rama por sí sola no guarda el árbol sin commit. El punto de recuperación se completa con commits locales revisados.
2. Separar, por rutas y bloques, los cambios previos de compra, autorización, pagos y boletos, y luego los de presentación pública. Mantener juntos los archivos que dependan entre sí y validar compilación para cada grupo.
3. Preparar por nombre los tres archivos exclusivos de la Entrega 3 y este documento. Seleccionar en `Home.razor` solo los dos enlaces y el cambio a la consulta compartida si los bloques previos ya quedaron registrados.
4. Revisar `git diff --cached --name-status`, `git diff --cached --check` y el diff preparado completo. Repetir el escaneo de secretos sobre el índice. Si un bloque queda mal preparado, retirarlo solo del índice mediante `git restore --staged -- <ruta>`. El archivo de trabajo conserva sus cambios.
5. Registrar cada grupo localmente tras aprobación. Tratar la expiración y conciliación como trabajo separado y repetir las pruebas pendientes antes de cerrar la disponibilidad para venta pública.
