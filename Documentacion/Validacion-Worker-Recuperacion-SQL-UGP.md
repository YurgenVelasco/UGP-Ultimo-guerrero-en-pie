# Validación del worker y recuperación SQL

Fecha: 2026-11-11. Instancia `(localdb)\MSSQLLocalDB`; base `UGP_Integracion_Test`.

## Dictámenes separados

| Área | Dictamen |
|---|---|
| Worker periódico | **PASA los casos ejecutados:** descubre vencidas, dos instancias concurrentes, preserva confirmadas, funciona con compras false y recupera agenda después de reconstruir el host. |
| Recuperación ante fallos SQL | **PASA desconexión de sesión y rollback real; cobertura parcial:** no se probó deadlock, pérdida de respuesta de COMMIT ni reintento automático EF del mismo comando interrumpido. |
| Integridad de inventario | **PASA en la base y fixtures examinados:** límites, liberación única, tickets y ausencia de duplicados comprobados. No equivale a una auditoría de toda combinación histórica posible. |
| Preparación para Wompi auténtico | **Preparado para planificar la prueba aislada, no habilitar checkout.** Falta secreto auténtico y autorización para conexión Sandbox; no hay evidencia de webhook real en esta entrega. |

## Entorno y precondiciones

Runner status confirmó baseline y UGP_0002 aplicados con hashes del manifiesto. No se ejecutó DDL ni UGP_0003. Base principal intacta. Las conexiones del harness se restringen al par exacto autorizado y exigen UGP_RESERVAS_TEST_APPROVED. Solo datos sintéticos y proveedor HTTP en memoria; ninguna llamada a Wompi ni secreto del comercio.

Se levantaron hosts genéricos de .NET con el ReservasWorker real, servicios reales y EF/SQL Server reales. No hay Kestrel, puertos, rutas públicas ni navegador. Configuración independiente en memoria, Development y Sandbox. Para preparar las cinco compras, se llamó al servicio de creación directamente con configuración sintética del harness; esto no habilita ningún checkout ni cambia configuración de la aplicación. Antes de arrancar los workers, Reservas:Habilitadas=false y Wompi:Webhooks:Habilitados=true únicamente en memoria del host aislado.

Los workers utilizan la estrategia EF de reintentos existente. Se ejecutan dos hosts distintos dentro del mismo proceso, con scopes/conexiones independientes. No se afirma que en esta entrega sean dos procesos del sistema operativo. El reinicio prueba detener/disponer hosts y construir uno nuevo con la misma SQL; no reinicia LocalDB ni Windows.

## Escenarios y evidencia SQL

Los fixtures 11–15 tienen un evento independiente de capacidad 5 y una entrada por compra. Resultados observados al terminar:

| Compra sintética | Escenario | Estado compra / reserva | Disponibles | Tickets | Resultado |
|---|---|---|---:|---:|---|
| 11 | Reserva vigente | Pendiente / Vigente | 4 | 0 | PASA, no se libera anticipadamente |
| 12 | Vencida, descubierta por dos workers | Expirada / Liberada | 5 | 0 | PASA, liberación única |
| 13 | Pago fixture PENDING y reserva vencida | Expirada / Liberada | 5 | 0 | PASA, no emite; sigue política de conciliación |
| 14 | Pago fixture aprobado y confirmado previamente | Aprobado / Confirmada | 4 | 1 | PASA, workers y reinicio no liberan ni duplican ticket |
| 15 | Fallo SQL, luego vencimiento y reinicio | Expirada / Liberada | 5 | 0 | PASA, rollback y recuperación sin doble liberación |

El barrido también puede procesar reservas sintéticas de pruebas anteriores que ya vencieron: son trabajo legítimo de esta base aislada. No se borraron fixtures ni se ajustaron inventarios para ocultar resultados. No se usó un supuesto de inventario global invariable: la liberación esperada es el comportamiento que se valida.

Se observó un segundo ciclo después de 11 segundos: la compra 15 continuó con disponibilidad 5 y cero tickets; la 14 conservó un ticket y disponibilidad 4. No hubo intervención del navegador.

## Fallo real de conexión, aislamiento y recuperación

Se abrió una sesión dedicada con Pooling=false y Application Name de prueba sobre la base exacta. Dentro de una transacción no confirmada se actualizó únicamente el evento de la compra 15 a disponibilidad 0. Después se inició un comando real `WAITFOR DELAY` y se cerró exclusivamente esa conexión desde el cliente mientras estaba en curso.

Resultado: el comando pendiente terminó con **InvalidOperationException**; una nueva conexión leyó compra Pendiente, cero tickets y disponibilidad original 4. SQL revirtió la actualización sin COMMIT. No fue el interceptor simulado de la etapa anterior: hubo transacción y comando en el motor y desconexión de su sesión. No se utilizó KILL, ALTER DATABASE, cambios de red, parada de instancia ni configuración global.

Después se hizo vencer ese fixture y se inició un host nuevo. El worker lo descubrió, liberó a 5 y un ciclo posterior conservó ese saldo. Esto prueba recuperación del trabajo persistido y una operación posterior satisfactoria tras rollback; **no demuestra reejecución automática EF del mismo UPDATE fallido** ni recuperación de un COMMIT cuyo resultado sea incierto. Esos escenarios siguen pendientes; no se equipara un cierre controlado del cliente a una caída de servidor o red.

## Integridad examinada

- Consulta EF global: ningún evento con inventario negativo ni superior a capacidad.
- Consulta EF global: ningún ticket asociado a compra cuyo EstadoPago sea distinto de Aprobado.
- Consulta EF global: ninguna reserva Liberada sin LiberadaUtc.
- Consulta SQL directa: cero grupos duplicados por (Entorno,WompiId).
- Consulta SQL directa: cero grupos duplicados de notificaciones por (TransaccionPagoId,Huella).
- Lectura directa de las cinco compras: coincide con la tabla anterior.

La invariancia de tickets válidos se comprobó para la compra confirmada y las no aprobadas de estos fixtures; el control global de EstadoPago no sustituye una auditoría histórica completa de firma QR, referencias, cobros y cantidades. No se detectaron operaciones parciales en la transacción deliberadamente interrumpida.

## Compilación, pruebas y cambios

Solo se modificó `Tests/Reservas/Program.cs` para añadir `--worker-sql`, preparar fixtures, arrancar hosts reales y comprobar la desconexión dedicada; se creó este documento. No fue necesario cambiar lógica de negocio. Un error inicial de compilación por un using de logging faltante en el harness se corrigió antes de ejecutar SQL.

Compilación final de aplicación y harness: **0 errores, 0 advertencias**. Las **30 comprobaciones aisladas** se ejecutaron y pasaron al inicio de la suite. Suite worker/SQL: código de salida 0, todas las comprobaciones descritas PASA. `git diff --check`: sin errores.

Reproducción, exclusivamente en la base aislada autorizada:

```powershell
dotnet build Tests/Reservas/Reservas.Checks.csproj --no-restore -o artifacts/worker-final
$env:UGP_RESERVAS_TEST_CONNECTION='Server=(localdb)\MSSQLLocalDB;Database=UGP_Integracion_Test;Integrated Security=true;Encrypt=false'
$env:UGP_RESERVAS_TEST_APPROVED='SCHEMA_REVIEWED_DISPOSABLE_DATABASE'
dotnet artifacts/worker-final/Reservas.Checks.dll --sql --worker-sql
Remove-Item Env:UGP_RESERVAS_TEST_CONNECTION,Env:UGP_RESERVAS_TEST_APPROVED
```

Cada ejecución agrega cinco compras sintéticas y conserva los datos; no es una prueba de solo lectura. El proveedor permanece simulado. La excepción de transporte es únicamente LocalDB, sin acreditar TLS de producción.

## Cierre y riesgos residuales

Hosts detenidos mediante finally; flags temporales false al finalizar. No se activó checkout, no se publicó receptor ni se cambiaron credenciales o flags persistidos. Fixtures conservados. UGP_0003 sigue bloqueado. Git: main, HEAD 62bdf1d, índice sin cambios preparados, trabajo preexistente preservado; sin commit ni push.

Pendientes: deadlock/reintento automático real del servicio, fallo de commit incierto, reinicio del proceso completo bajo carga y Wompi auténtico. El éxito de los casos ejecutados no certifica esas condiciones. **Detenerse y solicitar autorización antes de conectar Wompi Sandbox; no habilitar checkout.**
