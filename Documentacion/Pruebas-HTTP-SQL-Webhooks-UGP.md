# Pruebas HTTP/SQL del webhook UGP

Fecha: 2026-11-11. **Recepción HTTPS/SQL sintética validada; preparación para Wompi real todavía parcial. Checkout no habilitado.**

## Precondiciones y aislamiento

Instancia `(localdb)\MSSQLLocalDB`, base exacta `UGP_Integracion_Test`. Runner status informó baseline y UGP_0002 aplicados. El harness comprobó ambos hashes y ausencia de otras versiones en SchemaVersion; UGP_0003 no está registrado y no fue ejecutado por estas pruebas. Esto verifica el historial controlado, no descarta modificaciones manuales ajenas al runner.

Se usó ASP.NET Core/Kestrel real con controlador de producción WompiEventosController, ReservasService y EF Core/SQL real. Configuración independiente en memoria: se eliminan proveedores predeterminados, no se cargan User Secrets del comercio. Development, Sandbox, receptor true temporalmente, reservas false siempre, credenciales y secreto exclusivamente sintéticos. HttpClientFactory del proveedor no realiza llamadas de red: simula indisponibilidad. No hubo pagos ni comunicación con Wompi.

Kestrel escucha solo IPAddress.Loopback y puerto efímero con TLS. Certificado sintético de una hora importado para Schannel, sin instalar confianza global; el cliente de pruebas comprueba la huella exacta. El primer intento con clave efímera no importada falló en TLS antes de solicitudes, se detuvo el host y se corrigió exclusivamente el certificado del harness.

El host carga solo el controlador de eventos; no incorpora rutas Blazor, administrativas ni archivos estáticos. Esto prueba el aislamiento del host de pruebas, no una nueva autorización del sitio principal. Logs del host deshabilitados: no se imprimen payloads, secretos, SQL ni cadenas de conexión. Solo resultados e identificadores sintéticos.

Última ejecución: `https://127.0.0.1:55355`, reinicio en `https://127.0.0.1:65451`. Run sintético `http-ce04e570ad9c40739da44aeaa9bfee20`. Ejecución anterior satisfactoria: `http-af9cd1df7f8f497a9e75ab72269fe3c4`. Se conservaron sus filas de prueba. Todos los hosts se detuvieron y dispusieron en finally; flags en memoria descartados, receptor temporal puesto en false al terminar, sin cambiar configuración persistida.

## Hallazgo y corrección mínima

VerificarAsync todavía rechazaba la conciliación cuando Reservas:Habilitadas=false, aunque el worker y receptor ya aceptaban la política independiente. Se corrigió su guardia para permitir también WompiWebhookPolicy.Enabled. No se modificaron condiciones de importe/referencia/moneda, emisión ni liberación. La política mantiene Development/Sandbox/base local exacta y secreto de eventos requerido.

Archivos de esta etapa:

- Services/Implementations/ReservasService.cs: guardia de conciliación.
- Tests/Reservas/Program.cs: entrada `--http-sql`.
- Tests/Reservas/WebhookHttpChecks.cs: host y comprobaciones HTTP/SQL reproducibles.
- Este documento.

## Resultados observados

| Caso | HTTP | Resultado y evidencia SQL |
|---|---|---|
| Firma inválida | 401 | PASA; sin fila de transacción |
| Ambiente prod en host Sandbox | 401 | PASA; sin fila de transacción |
| Evento sintético firmado correctamente | 200 | PASA; transacción y una notificación visibles en consulta posterior a respuesta |
| Mismo evento repetido | 200 | PASA; sigue una sola notificación |
| Reencolado de estado terminal | 200 | PASA; versión notificada 2 y agenda no nula |
| Evento fuera de orden | 200 | PASA; estado terminal no se reemplaza por payload recibido |
| Fallo inyectado de SaveChanges | 503 | PASA; sin transacción parcial, sin aceptación exitosa |
| Reentrega al retirar el fallo | 200 | PASA; persistencia correcta |
| /admin, /checkout, /Sql/run.ps1 | 404 | PASA; rutas no publicadas en este host |
| Duplicado después de reiniciar host | 200 | PASA; dos notificaciones distintas totales, no tres |
| Conciliación tras reinicio, compras false | No aplica | PASA; servicio resolvió agenda persistida y guardó SinRespuestaVerificable con siguiente reintento |
| Inventario y tickets | No aplica | PASA; inventario por evento y cantidad total de tickets idénticos antes/después |

Para probar fuera de orden se fijó APPROVED exclusivamente en la bandeja sintética sin compra asociada, nunca en una compra ni en una respuesta de pago real. No se generaron tickets. Esto comprueba que la recepción no confía en el estado del payload; no acredita una aprobación del proveedor.

La inyección falla antes de SaveChanges mediante interceptor y se retira para la reentrega. El middleware de este host transforma excepciones en 503 sin exponer detalles. **No fue un error SQL Server transitorio real**, deadlock, desconexión ni prueba del backoff de EF. El pipeline web principal puede devolver 500 en vez de 503; el requisito de no confirmar con 200 se conserva porque el controlador espera el commit.

## Reinicio y límites del worker

Se detuvo y reconstruyó WebApplication/Kestrel con otro puerto, manteniendo la misma SQL. La prueba resuelve el servicio desde el nuevo host y ejecuta conciliación con Reservas:Habilitadas=false; observó persistencia de reintento con proveedor simulado offline.

**Worker periódico completo PENDIENTE:** el harness detectó dos reservas Vigente vencidas preexistentes en la última ejecución y no arrancó el barrido para no liberar esos fixtures durante la comprobación de inventario sin cambios. Se implementó una rama que registra el worker si no existen vencidas, pero esa rama no se ejecutó satisfactoriamente en esta entrega. No se modificaron fechas ni borraron fixtures para hacer pasar la prueba. Se necesita un plan separado que acepte sus liberaciones esperadas y verifique el delta de inventario, o una base limpia autorizada.

## Compilación y repetición

Compilación de aplicación y harness en artifacts/http-sql-worker: cero errores y advertencias. Suite aislada: 30 comprobaciones PASA. Suite HTTP/SQL terminó con código 0 con el pendiente explícito anterior. git diff --check sin errores.

Comandos desde la raíz, con permisos para acceder a LocalDB y certificados de usuario (sin cambiar ExecutionPolicy):

```powershell
dotnet build Tests/Reservas/Reservas.Checks.csproj --no-restore -o artifacts/http-sql-worker
dotnet artifacts/http-sql-worker/Reservas.Checks.dll --isolated
$env:UGP_RESERVAS_TEST_APPROVED='SCHEMA_REVIEWED_DISPOSABLE_DATABASE'
dotnet artifacts/http-sql-worker/Reservas.Checks.dll --http-sql
Remove-Item Env:UGP_RESERVAS_TEST_APPROVED
```

Cada ejecución añade IDs sintéticos nuevos; no limpia datos ni cambia esquema. El worker de prueba, si se inicia por no haber vencidas, puede reprogramar agendas sintéticas anteriores. No ejecutar sobre otra base ni cambiar el destino fijo del harness sin revisión.

## Riesgos, Git y dictamen

Pendientes: secreto auténtico, webhooks auténticos, proveedor Sandbox real, worker periódico tras reinicio con vencidas, fallo SQL real y recuperación/commit incierto. No se afirma que los eventos sintéticos sean comunicaciones Wompi.

No se tocó la base principal, no se ejecutó UGP_0003, no se modificaron secretos reales ni configuración de producción. No hay túnel ni escucha en interfaces públicas. El host principal no se inició. Rama main con cambios acumulados preservados; sin add, commit ni push.

**Dictamen: recepción HTTPS, persistencia, duplicados y conciliación manual tras reinicio comprobadas con SQL aislado; mantener receptor público y checkout deshabilitados.** No constituye aprobación completa de integración real Sandbox hasta resolver los pendientes.
