# Gobernanza SQL y preparación de pruebas UGP

Fecha: 2026-11-11. Entrega limitada a archivos. No se abrió ninguna conexión SQL, se ejecutaron scripts, migraciones, pagos ni se habilitó checkout.

## Inventario y alcance

- `Sql/UGP_Database_Schema.sql`: esquema histórico generado por EF, incluye `__EFMigrationsHistory` e InitialCreate. No demuestra qué se aplicó en el servidor. Se conserva íntegro.
- `Sql/UGP_0002_ReservasPagos.sql`: seis tablas de reservas/pagos, restricciones de inventario, índices y trazabilidad; instalación aditiva, preflight y transacción propia. Corregido previamente, sin ejecución demostrada.
- `Sql/UGP_0003_AdopcionReservasHistoricas.sql`: BLOQUEADO, intacto y excluido de la lista ejecutable. Requiere adopción histórica independiente con evidencia de pago, descuento original, tickets, huérfanos y aprobación por registro ambiguo.
- `Data/ReservasMapping.cs` y `Models/Entities/ReservaCompra.cs`: modelo esperado de reservas y pagos. Las pruebas aisladas revisan metadatos; no prueban compatibilidad del catálogo real.
- No existían runner ni SchemaVersion demostrables. `__EFMigrationsHistory` es una huella del esquema histórico, no sustituye el registro de scripts manuales. No se copió V0001 de otro proyecto.
- La configuración local declara UGP_Db_v2; no se consultaron credenciales privadas ni se confirmó el destino efectivo del proceso. Esa base no está autorizada por el runner.

## Infraestructura creada

| Archivo | Propósito |
|---|---|
| `Sql/Gobernanza.sql` | Prepara SchemaVersion y SchemaExecutionError, en transacción. |
| `Sql/manifest.json` | Identidades, orden y SHA-256 de los dos scripts permitidos; checksum del bootstrap; bloqueo explícito de UGP_0003; destinos vacíos. |
| `Sql/run.ps1` | Ejecutor Windows PowerShell 5.1 con autenticación integrada y TLS validado. |
| Este documento | Operación futura, pruebas y límites. |

SchemaVersion tiene ScriptId único, checksum, UTC de aplicación, resultado Applied y duración. Los fallos se registran separadamente con GUID, script, UTC y número de error, sin texto SQL que pueda revelar datos. La desconexión puede impedir registrar el fallo: no se promete una bitácora persistente si SQL está inaccesible. El bootstrap tampoco puede registrar su propio fallo antes de crear la tabla.

El runner valida identidades conocidas, orden creciente, duplicados, hashes y contrato transaccional. No descubre scripts mediante comodines. UGP_0003 no puede habilitarse añadiéndolo al JSON: la lista de identidades admitidas también está cerrada en el código.

El manifiesto requiere revisión humana y control Git; el hash detecta deriva accidental, no es una firma contra un atacante que pueda editar simultáneamente runner y manifiesto. Después de aplicar una versión no se cambia su contenido: se prepara otra versión revisada.

## Modos y seguridad de ejecución

- `validate`: solo archivos, sin conexión SQL.
- `status -Offline` y `plan -Offline`: inventario local, estado SQL explícitamente desconocido; no afirman qué está aplicado.
- `status` y `plan` sin Offline: requieren servidor/base exactos registrados; leen DB_NAME, catálogo y versiones. No crean tablas ni toman el bloqueo de escritura.
- `apply`: requiere destino exacto registrado, nombre `UGP_Reservas_Test_*` y `-Authorization 'APPLY:servidor/base'`. No se ejecutó en esta etapa. No admite Offline.

Actualmente `targets` está vacío: no hay destino ejecutable. Registrar un destino solo después de autorización y verificación del aislamiento. La confirmación es una barrera operativa, no reemplaza permisos SQL; usar cuenta separada de despliegue y nunca conceder DDL a la aplicación pública.

Apply mantiene una sola conexión y un applock exclusivo de sesión `UGP:Governance`. Si otra ejecución tiene el bloqueo, falla sin esperar. El bootstrap solo se permite sin tablas de usuario; bases existentes requieren un procedimiento independiente de adopción, no un baseline automático.

Cada script conserva su propia transacción: el runner inserta SchemaVersion inmediatamente antes del único COMMIT reconocido. El hash corresponde al archivo original revisado; la instrumentación de registro es explícita. No se envuelve UGP_0002 en una transacción externa. Los parámetros SESSION_CONTEXT confirman la base y revisión requerida por UGP_0002.

Se separan lotes por GO en línea independiente. Este es un ejecutor de estos scripts con hashes conocidos, no un intérprete SQLCMD general: no admite GO con repetición, variables SQLCMD ni archivos arbitrarios. El baseline crea __EFMigrationsHistory antes de su transacción; un fallo puede dejar esa tabla vacía. No se afirma atomicidad de toda la instalación: cada script y su registro son una unidad, y el bootstrap es otra.

No hay reintento DDL automático. Ante timeout/deadlock/fallo, se intenta rollback y registro del número de error. Ante resultado incierto del COMMIT, consultar versiones y objetos antes de reintentar. Una versión aplicada con hash distinto, desconocida o fuera de orden bloquea la ejecución.

## Validación local

- Compilación principal en `artifacts/gobernanza-web`: cero errores y advertencias. Se usa salida separada para conservar el ejecutable abierto.
- Parser de PowerShell: cero errores sintácticos.
- SHA-256 del baseline, UGP_0002 y bootstrap: coinciden con manifiesto.
- Proyecto de pruebas compilado con cero errores y advertencias; 22 comprobaciones aisladas PASA (políticas de conciliación, firmas de fixtures y metadatos EF), sin conectar SQL.
- `git diff --check`: sin errores en los cambios registrados. Los archivos nuevos se revisan separadamente porque Git no los incluye en ese comando hasta prepararlos.
- Ejecución de `run.ps1 -Mode validate` y `plan -Offline`: BLOQUEADA por AuthorizationManager de Windows. No se cambió la política ni se eludió el bloqueo. Por tanto no se declara probado el comportamiento ejecutado del runner.
- Las pruebas SQL, rollback, bloqueo de dos runners y registro atómico quedan pendientes del entorno aislado. Una revisión del código no sustituye esas pruebas.

## Plan reproducible de integración, pendiente de autorización

1. DBA identifica instancia y una base nueva desechable `UGP_Reservas_Test_<identificador>`, propietario, certificado TLS y cuenta integrada. Verificar nombre efectivo, ausencia de usuarios/productores conectados y ausencia de datos reales. No apuntar a UGP_Db_v2 ni clonar datos personales sin anonimización.
2. Aprobar creación de esa base por separado; este runner no crea bases. Para cualquier origen existente, respaldo COPY_ONLY con CHECKSUM y restauración de ensayo; VERIFYONLY por sí solo no acredita recuperación. Conservar scripts/hashes y evidencia antes/después.
3. Registrar exactamente servidor/base en targets. Resolver firma/confianza del script según política Windows administrada, sin desactivar protecciones. Ejecutar validate, status y plan; revisar salida antes de autorizar apply.
4. Aplicar solo bootstrap, baseline y UGP_0002. Verificar tablas base Eventos/Compras/Tickets, seis tablas nuevas, FKs, índices, restricciones confiables, tipos y SchemaVersion. Repetir plan: cero pendientes. Segundo apply no debe modificar filas. Hash alterado, ID duplicado y destino desconocido deben rechazarse en copias de prueba.
5. Probar dos runners simultáneos; fallo DDL y pérdida de conexión antes/después del commit. Verificar ausencia de versión falsa y recuperación sin repetir efectos. No editar scripts ya registrados para simular errores: usar una copia desechable del ejecutor y una base exclusivamente destinada a esas pruebas.
6. Ejecutar harness SQL existente únicamente con su conexión privada y confirmación de base desechable. Revisar primero sus precondiciones y fixtures. No sustituir estas pruebas por mocks.
7. Casos: reserva vigente; TTL vencido/abandono; rechazo verificable; aprobación antes del vencimiento; dos compradores por último cupo; expiración doble; notificación duplicada/fuera de orden; aprobación/expiración simultáneas; pago tardío tras liberar; error SQL y reintento; reinicio del worker con agenda pendiente.
8. Invariantes tras cada caso: disponibilidad entre cero y capacidad; una única disminución por reserva; una única restitución por liberación; aprobación sin nuevo descuento; unicidad de tickets/transacción; ningún ticket activo por compra no aprobada; pago tardío registrado para conciliación sin reactivar cupos reasignados. Registrar conteos antes/después y resultados de ambas tareas concurrentes.
9. El harness cubre parte de estos casos; reinicio del host/worker real, notificación HTTPS y fallo de conexión requieren pruebas adicionales. Mantener checkout de la aplicación habitual deshabilitado y usar fixtures desechables; no realizar pagos reales.
10. Recuperación: detener exclusivamente procesos de prueba, conservar evidencias, restaurar en otra base aislada y verificar invariantes. La eliminación requiere autorización independiente, comprobación de servidor/DB_NAME exactos y ausencia de conexiones ajenas; no se proporciona ni ejecuta DROP automático. Nunca restaurar encima de pagos posteriores al respaldo.

## Comandos futuros (no ejecutados contra SQL)

Desde la raíz, después de autorización y resolución de la política de ejecución:

```powershell
.\Sql\run.ps1 -Mode validate
.\Sql\run.ps1 -Mode plan -Offline
# Sustituir por el destino aislado verificado y registrarlo exactamente en targets:
$testServer = '<INSTANCIA_APROBADA>'
$testDatabase = 'UGP_Reservas_Test_<ID_APROBADO>'
.\Sql\run.ps1 -Mode status -Server $testServer -Database $testDatabase
.\Sql\run.ps1 -Mode plan -Server $testServer -Database $testDatabase
# Solo con una autorización posterior explícita:
.\Sql\run.ps1 -Mode apply -Server $testServer -Database $testDatabase -Authorization "APPLY:$testServer/$testDatabase"
```

Para el harness, establecer privadamente `UGP_RESERVAS_TEST_CONNECTION` y la variable de aprobación indicada en `Tests/Reservas/Program.cs`; no guardar cadenas en archivos versionados ni transcribirlas a informes. Construir el proyecto de pruebas y ejecutar su DLL con `--sql` solo después de revisar ese contrato y autorizar escrituras de fixtures.

## Dictamen y Git

Infraestructura preparada, NO validada en SQL Server ni autorizada para aplicación. UGP_0002 compatible por inspección con la instrumentación; pendiente de DDL real. UGP_0003 sigue BLOQUEADO. No se adopta ningún histórico ni se acredita gobernanza previa de la base actual.

Rama main, un commit local por delante de origin/main; se conservan los cambios acumulados de entregas anteriores. Estos cuatro archivos son nuevos y no preparados en el índice. No se hizo add, commit ni push. El baseline y UGP_0003 conservan sus hashes anteriores. El manifiesto no contiene credenciales y su lista de destinos está vacía.

Condición de parada: solicitar autorización para provisionar/verificar una base SQL Server aislada y ejecutar allí el plan; no proceder en la base actual.
