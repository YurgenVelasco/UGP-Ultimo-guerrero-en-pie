# Validación del checkout Sandbox UGP

Fecha: 2026-11-11. Dictamen actualizado: **SQL DE PRUEBAS VALIDADO EN LOS CASOS INDICADOS; CHECKOUT PERMANECE BLOQUEADO**.

## Actualización: excepción local autorizada y pruebas ejecutadas

Esta sección reemplaza los estados de ejecución anteriores; las secciones posteriores conservan el diagnóstico inicial como trazabilidad.

### Transporte y aislamiento

El runner usa **System.Data.SqlClient**, no Microsoft.Data.SqlClient ni Encrypt=Strict. Estaba configurado con Encrypt=True y TrustServerCertificate=False. El error inicial fue falta de soporte de cifrado. La prueba controlada con Encrypt=True y TrustServerCertificate=True también falló en prelogin por Named Pipes («No hay ningún proceso en el otro extremo de la canalización»). No hay evidencia para atribuirlo exclusivamente a una cadena de certificados.

Se implementó Encrypt=False únicamente cuando servidor y base coinciden con comparación sensible a mayúsculas con `(localdb)\MSSQLLocalDB` y `UGP_Integracion_Test`, y la autenticación es Windows integrada. El manifiesto sigue registrando solo ese par. Todos los demás destinos conservan Encrypt=True/TrustServerCertificate=False y requieren autorización del manifiesto. Plan rechazó tanto `otro-servidor` como `OtraBase` antes de conectar. No se alteraron configuraciones globales, PowerShell ni conexiones de la aplicación.

La suite utiliza Microsoft.Data.SqlClient con esa conexión explícita de pruebas mediante variables de proceso; no se guardaron credenciales. Estas pruebas LocalDB **no verifican TLS de producción**. Referencia consultada: [Microsoft: LocalDB y desarrollo local](https://learn.microsoft.com/en-us/sql/connect/ado-net/sql/sqlclient-support-localdb?view=sql-server-ver16).

### Aplicación real

Se comprobó nuevamente ausencia y se creó únicamente UGP_Integracion_Test. El plan mostró baseline y UGP_0002 pendientes. Apply ejecutó Gobernanza.sql, UGP_Database_Schema.sql para inicializar la base vacía y UGP_0002. Los SHA-256 son los de la tabla histórica de este informe, sin cambios. UGP_0003 NO se ejecutó. Plan posterior mostró ambas versiones aplicadas y ningún pendiente; SchemaVersion registra ambos hashes y resultado Applied.

### Resultados reales con SQL y proveedor simulado

- PASA: reserva vigente, expiración en dos procesos y liberación duplicada sin incremento doble.
- PASA: rechazo duplicado libera una vez; PENDING vencido no emite; aprobación tardía no recupera inventario y crea una incidencia única.
- PASA: recepción duplicada mantiene una sola transacción; confirmaciones simultáneas emiten una vez; ID terminal se reprograma; respuesta antigua no revierte aprobación.
- PASA: carrera aprobación/expiración mantiene un resultado consistente; transacción ajena no aprueba otra compra.
- PASA: fallo del proveedor no retiene inventario; dos compradores disputando el último cupo dejan un único ganador sin inventario negativo.
- PASA mediante reanudación: fallo inyectado durante emisión conserva compra pendiente, cero tickets e inventario reservado; reintento y verificación repetida emiten una vez.
- PASA: un proceso independiente nuevo recupera una reserva vencida. No equivale a reiniciar SQL Server ni a reiniciar el host web/worker completo.

La primera ejecución se interrumpió por un defecto del harness: EF envuelve InjectedFailure en DbUpdateException. Se corrigió la captura sin cambiar negocio. Se añadió `--resume-rollback`, restringido al destino exacto, para comprobar y continuar el último fixture sin borrar datos. Los intentos iniciales de reanudación fallaron al suponer que la bandeja conservaba la asociación al intento; la asociación también había sido revertida. La reanudación final comprobó el estado de la última orden antes de escribir y terminó con código 0. No se afirma una ejecución completa de principio a fin del harness corregido sobre otra base vacía.

Compilación final de aplicación y harness en artifacts/transporte-recovery: cero errores y advertencias. Las 22 comprobaciones aisladas volvieron a pasar. Las pruebas usan respuestas Wompi HTTP en memoria, no pagos ni confirmaciones de producción. Fixtures sintéticos conservados en la base, sin eliminación ni modificación de la principal.

### Pendientes y dictamen

Pendientes: secreto auténtico Wompi:EventsSecret, prueba real de webhook/consulta Sandbox, reinicio del host/worker completo, desconexión SQL/commit incierto y ejecución simultánea de dos runners. La prueba de rollback inyecta un fallo cliente antes de emitir; no reproduce un deadlock o corte físico SQL. Se recomienda repetir la suite corregida de principio a fin en un entorno limpio autorizado, sin limpiar automáticamente esta base.

Cambios de esta intervención: Sql/run.ps1 (excepción limitada), Tests/Reservas/Program.cs (captura y reanudación de pruebas) y este documento. Manifiesto y scripts mantienen los valores aprobados de la etapa anterior. Rama main, cambios preexistentes preservados, sin add/commit/push. Checkout y flag no habilitados; secretos sin cambios.

**Dictamen: los casos transaccionales ejecutados pasan; mantener checkout bloqueado hasta completar integración Wompi y obtener autorización posterior.**

## Entorno y límite de ejecución

Instancia autorizada: `(localdb)\MSSQLLocalDB`, versión 17.0.4025.3. Estaba detenida. La consulta inicial desde el aislamiento falló al acceder al registro de LocalDB; fuera del aislamiento se pudo identificar la instancia y conectar al catálogo `master` con autenticación integrada. La conexión inició LocalDB.

Consulta real de solo lectura: `SELECT COUNT(*) FROM sys.databases WHERE name=N'UGP_Integracion_Test'`; resultado **0**. No se creó esa base, no se reemplazó ninguna base y no se accedió a tablas de la base principal. La consulta de descubrimiento empleó SqlClient con su configuración predeterminada de transporte local; no acredita una conexión cifrada.

El runner exige `Encrypt=true` y `TrustServerCertificate=false`. Al ejecutar plan contra el destino propuesto, SQL respondió: **«La instancia de SQL Server con la que intenta conectar no admite cifrado.»** Por ello las precondiciones de aplicación no se cumplieron. No se ejecutó apply ni DDL. No se redujo el requisito de cifrado para continuar silenciosamente.

## Cambios mínimos realizados

- `Sql/manifest.json`: único destino registrado `(localdb)\MSSQLLocalDB` / `UGP_Integracion_Test`.
- `Sql/run.ps1`: excepción exacta para ese par instancia/base, conservando la lista de destinos; corrección de asignación de SqlConnectionStringBuilder mediante nombres canónicos (`Data Source`, `Initial Catalog`, `Integrated Security`, `Encrypt`, `TrustServerCertificate`). La sintaxis anterior fallaba en Windows PowerShell con «Palabra clave no admitida: DataSource».
- `Tests/Reservas/Program.cs`: acepta ese mismo par exacto para futuras pruebas, conservando la confirmación adicional por variable de entorno y el rechazo de bases con datos.
- Este informe.

No se modificaron scripts SQL, credenciales, flags ni código de compras/pagos. Los prefijos de prueba anteriores siguen requiriendo registro explícito en el runner. No se amplió la autorización a otras instancias para la base nueva.

## Scripts y hashes SHA-256

**Scripts aplicados: ninguno.** Los siguientes son archivos verificados, no versiones aplicadas:

| Archivo en Sql | SHA-256 | Estado |
|---|---|---|
| Gobernanza.sql | 7822C1D3AA1695CD8AEAA1E26F14EC9093FACF560FE1360BC1BB99F67D9E2850 | Sin aplicar |
| UGP_Database_Schema.sql | A7FE08D90752E1CF438FEDE2D574810368973B42D4284E62E289437D999FB3E1 | Sin aplicar; inicialización necesaria de la base vacía |
| UGP_0002_ReservasPagos.sql | 9E801B8D855A9AE5F9AD24A7B0A7B961DE75988213A3CC06324A6E95EC0B6320 | Sin aplicar |
| UGP_0003_AdopcionReservasHistoricas.sql | 7A634E2B182ED2062E364FD9FDCA8986F3A531915DC3B846B9E5B76C04352CBC | BLOQUEADO, intacto, fuera de ejecución |

## Resultados observados

| Prueba | Resultado | Evidencia y alcance |
|---|---|---|
| Identificación de LocalDB | PASA | Instancia y versión obtenidas fuera del aislamiento |
| Ausencia de base de pruebas | PASA | Catálogo master devuelve 0 |
| Runner validate | PASA | Baseline y UGP_0002, hashes y contrato local validados |
| Runner destino no autorizado | PASA | Plan con UGP_NoAutorizada rechazado antes de conectar |
| Runner plan del destino autorizado | BLOQUEADA | Error de cifrado; no alcanzó lectura de SchemaVersion |
| Compilación aplicación y harness | PASA | `dotnet build Tests/Reservas/Reservas.Checks.csproj --no-restore -o artifacts/validacion-sandbox`: 0 errores, 0 advertencias |
| Pruebas aisladas | PASA | 22 comprobaciones mediante DLL `--isolated` |
| git diff --check | PASA | Sin errores |
| Creación / bootstrap / UGP_0002 | NO EJECUTADA | Precondiciones del runner no satisfactorias |
| Concurrencia, liberación única, idempotencia SQL | BLOQUEADA | No existe base inicializada |
| Pago tardío e integridad de tickets SQL | BLOQUEADA | No existe base inicializada |
| Recuperación tras reinicio SQL/worker | BLOQUEADA | Sin ejecución del harness SQL ni host de prueba |

PowerShell produjo AuthorizationManager dentro del aislamiento, pero el mismo `powershell -NoProfile -File ...` funcionó fuera de él con permisos autorizados. No se cambió ExecutionPolicy, no se usó Bypass ni se desbloquearon archivos. La política observada de LocalMachine era RemoteSigned. El bloqueo actual demostrado es de conexión cifrada, no un rechazo del script por política de Windows.

Por revisión estática, el runner comprueba IDs/orden/hash, lista cerrada, historial y PK de SchemaVersion; utiliza applock de sesión y registro antes del COMMIT del script. La exclusión mutua real, rechazo de historial alterado, atomicidad del registro y comportamiento ante desconexión **no están probados en el motor**. No se equiparan esas lecturas a pruebas de concurrencia aprobadas.

## Wompi y configuración privada

El diagnóstico anterior constató ausencia de `Wompi:EventsSecret`, flag de reservas ausente/false y formato Sandbox de las otras tres llaves. No se cambiaron ni consultaron valores privados nuevamente para este informe. No hubo llamadas a Wompi ni pagos. Las 22 pruebas usan fixtures locales; no demuestran credenciales válidas, un comercio común o webhooks reales.

ASP.NET Core carga User Secrets del proyecto en Development mediante CreateBuilder. Para configurar posteriormente el secreto auténtico de eventos Sandbox sin imprimirlo ni incluirlo en historial, el operador puede usar una consola privada desde la raíz y entrada enmascarada:

```powershell
$privateEvent = Read-Host 'Secreto auténtico de eventos Sandbox' -AsSecureString
$ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($privateEvent)
try {
    @{ 'Wompi:EventsSecret' = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) } |
        ConvertTo-Json -Compress | dotnet user-secrets set
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
    Remove-Variable privateEvent,ptr
}
```

No se ejecutó ese comando. User Secrets es almacenamiento local privado, no un almacén cifrado de producción. No usar `user-secrets list` en registros compartidos, ni valores de ejemplo. Tener el secreto no autoriza habilitar el flag.

## Requisitos para continuar

1. Resolver explícitamente el transporte del entorno de pruebas: una instancia con cifrado válido, o revisar y autorizar una excepción de transporte exclusivamente local para esta instancia/base. No se aplicó ninguna excepción y no se seleccionó otra instancia sin autorización.
2. Repetir identificación de instancia y ausencia de UGP_Integracion_Test antes de crearla; la observación de hoy no evita carreras futuras. No reemplazarla si aparece.
3. Validar runner, crear base vacía autorizada, inicializar baseline propio y gobernanza, aplicar solo UGP_0002 como cambio de reservas. UGP_0003 permanece excluido.
4. Ejecutar harness SQL con datos sintéticos: último cupo concurrente, liberación repetida, confirmación repetida, pago tardío, rollback y nuevo proceso. Probar adicionalmente concurrencia del runner y recuperación del worker real. Conservar resultados y fixtures sin borrarlos automáticamente.
5. Verificar secreto auténtico y entrega de notificaciones Sandbox, verificación backend e inventario/tickets. Solo tras resultados satisfactorios solicitar autorización separada para habilitar checkout Sandbox.

## Riesgos y Git

No se ha demostrado idempotencia ni consistencia transaccional sobre SQL real. No hay base de integración creada ni resultados de aplicación. Los cambios históricos de inventario siguen fuera del alcance. La aplicación principal y su checkout permanecen sin alteraciones de activación.

Rama main, HEAD 62bdf1d, un commit por delante de origin/main según referencia local. Cambios acumulados preservados; no se preparó el índice, no se hizo commit ni push. El runner/manifiesto y harness ya eran archivos sin seguimiento antes de esta intervención. Únicas modificaciones de esta etapa: los tres archivos enumerados y este documento nuevo.

**Dictamen final: MANTENER BLOQUEADO.** Detención antes de crear la base o ejecutar SQL de esquema porque la conexión requerida por la gobernanza no fue satisfactoria.
