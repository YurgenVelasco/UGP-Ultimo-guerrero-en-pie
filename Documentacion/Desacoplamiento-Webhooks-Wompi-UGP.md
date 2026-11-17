# Desacoplamiento seguro del webhook Wompi

Fecha: 2026-11-11. Implementación preparada; receptor y checkout públicos NO habilitados.

## Archivos de esta intervención

- `Services/Implementations/WompiWebhookPolicy.cs`: nueva política independiente y cerrada por defecto.
- `Controllers/WompiEventosController.cs`: usa la política del receptor en lugar del flag de compras.
- `Services/Implementations/ReservasWorker.cs`: permite procesamiento de reservas existentes cuando la política aislada está habilitada, aunque compras estén deshabilitadas.
- `Services/Implementations/ReservasService.cs`: permite el barrido de expiración bajo esa misma política; sin cambiar las transacciones ni reglas de emisión.
- `Program.cs`: registro scoped de la política.
- `Tests/Reservas/Program.cs`: ocho comprobaciones aisladas adicionales.
- Este documento.

## Configuración y límites

Nuevo flag: `Wompi:Webhooks:Habilitados`, false cuando no está configurado. No se añadió true a ningún archivo ni a User Secrets.

Para que la política permita recepción deben cumplirse simultáneamente:

1. Flag explícito true.
2. Host ASP.NET Core Development.
3. Wompi:Environment igual a Sandbox.
4. Wompi:EventsSecret con formato Sandbox aceptado por EventsConfigured.
5. DefaultConnection apunta exactamente a `(localdb)\MSSQLLocalDB` y `UGP_Integracion_Test`, con autenticación integrada, sin AttachDBFilename.

El chequeo de formato no puede acreditar por sí solo que el secreto provenga del comercio. No se genera ni instala un sustituto: el responsable debe suministrar el secreto auténtico mediante configuración privada. Solo una notificación real validada acredita la interoperabilidad con Wompi. El secreto auténtico seguía pendiente según la revisión anterior; no se cambiaron ni imprimieron credenciales en esta entrega.

`Reservas:Habilitadas` continúa controlando la creación de compras. El nuevo flag no lo modifica. Se conserva el bloqueo de CompraService. No se inició el host, no se creó túnel ni se publicaron rutas.

El worker evalúa la habilitación al arrancar: tras configurar un host de pruebas será necesario reiniciarlo. Con la política aislada habilitada procesa conciliación y expiración de reservas existentes, no crea compras. El contexto SQL utilizado por el proyecto sigue procediendo de DefaultConnection. No se cambió su valor ni se accedió a ninguna base en esta entrega.

## Autenticación, persistencia y conciliación

El controlador exige HTTPS; con receptor deshabilitado devuelve 503. Con receptor habilitado y evento inválido devuelve 401. ValidateEvent conserva SHA-256 sobre valores de signature.properties en su orden, timestamp y secreto, comparación en tiempo constante, transaction.updated y ambiente test.

La recepción válida espera RecibirNotificacionAsync antes de responder 200. Ese método conserva transacción SQL, bloqueo por entorno/ID, unicidad de transacción y huella, e incremento de versión para recepciones nuevas. Los errores de persistencia se propagan: no se responde 200 antes del commit. No se añadió una respuesta de éxito ante excepciones. Las recepciones repetidas conservan la agenda existente; una recepción distinta reencola el ID registrado sin degradar el estado terminal.

Recibir el evento no aprueba una compra. La conciliación sigue consultando Wompi y contrastando referencia, importe, moneda, identidad y estado. Una reserva vencida se libera; una aprobación tardía genera incidencia sin reactivar inventario ni emitir tickets. Esas reglas permanecen en ReservasService y no se reemplazaron por el estado recibido en el webhook.

## Pruebas ejecutadas y pendientes

Build de aplicación y harness: **0 errores, 0 advertencias** (`artifacts/webhook-separado`). Suite `--isolated`: **30 comprobaciones PASA**. `git diff --check`: sin errores.

| Caso | Resultado | Tipo de evidencia |
|---|---|---|
| Receptor deshabilitado | PASA | Invocación directa del controlador: 503 |
| Receptor habilitable con compras false | PASA | Política con configuración en memoria de pruebas; no se habilitó la app |
| Secreto ausente | PASA | Política false y controlador 503 |
| Firma inválida | PASA | Controlador devuelve 401 sin persistencia |
| Ambiente de evento producción | PASA | Controlador devuelve 401 |
| Host Production / base principal / servidor remoto | PASA | Política rechaza los tres casos |
| Firma Sandbox de fixture | PASA | Prueba aislada existente; NO es evento auténtico de Wompi |
| Duplicados y eventos fuera de orden | PASA aislado | ConciliacionPolicy; unicidad del modelo EF. Evidencia SQL anterior en Validacion-Checkout-Sandbox-UGP.md, no repetida aquí |
| Evento auténtico Wompi | PENDIENTE | Falta secreto auténtico y receptor HTTPS de prueba |
| Respuesta 200 después de persistencia real con compras false | PENDIENTE | Orden de operaciones revisado estáticamente; no prueba HTTP/SQL nueva |
| Fallo transitorio de persistencia y reentrega | PENDIENTE | Propagación de error revisada; no se inyectó fallo en el receptor HTTP |
| Reinicio del host con compras false y conciliación | PENDIENTE | Worker adaptado; no se inició host ni se ejecutó SQL en esta intervención |

Las pruebas del controlador son invocaciones de método con DefaultHttpContext, no peticiones por Kestrel ni pruebas de accesibilidad externa. No se atribuyen las pruebas SQL previas a este nuevo modo independiente.

## Riesgos y pasos siguientes

Se requiere el secreto auténtico, montaje HTTPS aislado y pruebas HTTP/SQL del nuevo modo. Un proxy TLS deberá preservar el esquema HTTPS mediante configuración de confianza limitada. No publicar toda la aplicación: limitar exposición al endpoint de eventos, sin checkout, SQL, runner ni recursos privados.

La barrera de base/entorno reduce errores de configuración; no sustituye permisos de SQL, aislamiento del proceso ni verificación del destino en despliegue. La validez del comercio y de las credenciales solo se comprobará contra Sandbox real. La actualización dinámica del flag no inicia un worker que salió al arrancar; usar reinicio controlado.

Git: rama main, un commit por delante de origin/main según referencia local; cambios anteriores preservados, sin preparar índice, commit ni push. No se ejecutó UGP_0003 ni se modificaron esquemas, bases, credenciales o lógica de compras.

**Dictamen: desacoplamiento implementado y validado aisladamente; mantener receptor público y checkout deshabilitados.** La integración real y la recuperación del host en este modo quedan pendientes de validación, no aprobadas.
