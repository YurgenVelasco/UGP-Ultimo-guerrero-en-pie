# Remediación previa a ejecución SQL — UGP

Fecha: 11 de noviembre de 2026. Proyecto: `C:\Users\Yurgen Velasco\source\repos\UGP(ultimo guerrero en pie)\UGP(ultimo guerrero en pie)`.

**No se ejecutó SQL, no se consultó ninguna base, no se inició la aplicación ni se habilitaron compras.** UGP_0003 y el baseline permanecen intactos. No se creó un runner ni una tabla SchemaVersion. Esta entrega prepara código y SQL para revisión; no acredita su funcionamiento en SQL Server real.

## 1. Dictámenes separados

| Área | Dictamen y evidencia |
|---|---|
| UGP_0002 | **CORREGIDO EN ARCHIVO; EJECUCIÓN BLOQUEADA** hasta aprobar gobernanza y probar el DDL en SQL Server desechable. Tiene preflight, transacción propia, índices faltantes, FK por entorno y trazabilidad. No se certificó sintaxis/ejecución con el motor ni compatibilidad de filas reales. |
| UGP_0003 | **BLOQUEADO, SIN MODIFICACIONES.** No hay inventario real de históricas ni manifiesto conciliado y aprobado. No se infirieron pagos ni fechas. |
| Conciliación Wompi | **REMEDIADA EN CÓDIGO Y PRUEBAS AISLADAS; INTEGRACIÓN PENDIENTE.** Veintidós comprobaciones aisladas pasan, incluidas generación de agenda y respuestas fuera de orden. Exclusión mutua, unicidad e idempotencia reales siguen pendientes de SQL y HTTPS Sandbox. |
| Gobernanza SQL | **PROPUESTA PARA APROBACIÓN, NO IMPLEMENTADA.** Se identificó la infraestructura histórica del repositorio; no hay runner ni contrato SchemaVersion demostrable. Tampoco evidencia del catálogo/aplicaciones reales del servidor. |

## 2. Defectos originales y tratamiento

| Hallazgo de auditoría | Remediación | Límite pendiente |
|---|---|---|
| A-01: falta de gobernanza | Se documenta el contrato mínimo propuesto y se agrega confirmación explícita del destino/revisión como precondición del script. | No se inventó ni creó SchemaVersion. Integrarlo exige aprobación del contrato y revisión del catálogo real. |
| A-02/A-03/A-09: adopción histórica sin evidencia y repetición ambigua | UGP_0003 congelado; plan de adopción en sección 7. | No se corrige ni ejecuta ese script. |
| A-04: notificación existente no reprogramada | Recepciones auténticas deduplicadas por huella; versión notificada y procesada; cualquier recepción distinta reencola el ID existente. | Pruebas SQL/worker real pendientes. |
| A-05: un elemento fallido bloquea el lote | Conciliación captura error por ID, persiste siguiente intento y continúa. Expiración guarda ReintentarLiberacionUtc y excluye temporalmente reservas fallidas del lote. | Si SQL completo está caído no puede persistir el reintento; el ciclo vuelve a intentar. Monitorización y pruebas de carga pendientes. |
| A-06: errores de bloqueo convertidos a un único código | Helper común distingue 51011 timeout, 51013 deadlock, 51012 cancelación y 51019 fallo no transitorio. Solo 51011/51013 se agregan a reintentos EF. | No se produjo deadlock SQL real ni fallo de commit incierto en esta entrega. |
| A-07: índices de relación/agenda ausentes | Índice compuesto por intento/entorno y agendas por entorno/fecha, con filtro NOT NULL; SQL y EF alineados. | Planes y tiempos sin medir. |
| A-08: CHECK sobre históricas | Preflight rechaza inventario incompatible sin alterarlo, seguido de WITH CHECK en la transacción. | Calidad real del inventario y respaldo restaurable desconocidos. |
| A-10: script sin preflight/gestión explícita de errores | Rechaza transacción externa, destino no confirmado, versión SQL insuficiente, objetos existentes/parciales y columnas base incompatibles. TRY/CATCH, XACT_STATE, rollback y applock de despliegue. | Errores de compilación requieren además un ejecutor que corte la sesión; no se atribuye cobertura absoluta a XACT_ABORT. |
| A-11: fecha de verificación sin consulta e historial insuficiente | El worker sin ID ya no escribe UltimaVerificacionUtc. Se agregan recepciones y observaciones de verificación persistentes, separadas del estado efectivo. | Retención, permisos e interfaz administrativa de consulta pendientes. |
| A-12: coherencia de entorno/estados/fechas | FK compuesta Id/Entorno, CHECK de estados, versiones y orden temporal; esquema dbo explícito y CHECK también descritos en EF. | Las restricciones cruzadas de inventario completo aún dependen del servicio transaccional y de excluir escritores antiguos. |
| A-13: cobertura insuficiente | Política real compartida por servicio y pruebas aisladas; nuevas comprobaciones de reencolado, carrera de generaciones, terminales y metadatos EF. Preparado caso SQL de ID terminal reencolado. | No se equiparan pruebas puras a pruebas de worker, red, deadlock ni servidor. |

## 3. UGP_0002: cambios concretos y compatibilidad

Continúa siendo una instalación aditiva de esquema nuevo; no elimina tablas/columnas ni migra compras. El baseline no se modifica. Al no haberse aplicado esta propuesta, se corrige el mismo archivo; si alguien la aplicó fuera de esta sesión, **no ejecutar esta revisión como si fuera actualización**: rechaza objetos existentes y se necesitaría otro script diferencial revisado.

Precondiciones nuevas:

- Conexión dedicada sin transacción externa. El script es propietario del BEGIN/COMMIT; no envolverlo sin revisar ese contrato.
- `SESSION_CONTEXT('UGP.BaseRevisada')` debe coincidir con DB_NAME y `UGP.AutorizacionDDL` con `UGP_0002_REVISION_2`. No se configuraron estas marcas. Son barreras contra errores accidentales, no credenciales ni autorización de seguridad.
- SQL Server 2016 o posterior para las funciones utilizadas. Compras.compra_id y las columnas base de inventario de Eventos deben ser int NOT NULL bajo dbo.
- Ninguno de los seis objetos nuevos ni CK_Eventos_Inventario debe existir. No se omiten silenciosamente estructuras parciales ni se declara idempotente el despliegue por encontrar una tabla.
- Inventario previo no negativo ni superior a capacidad. La lectura bloqueada y el CHECK posterior permanecen en la misma transacción; no se recortan ni recalculan saldos.

El script conserva ReservasCompra, IntentosPago, TransaccionesPago e IncidenciasPago, y agrega soporte de la remediación:

| Cambio | SQL / EF |
|---|---|
| Reserva.ReintentarLiberacionUtc | datetime2 nullable / DateTime?; difiere una anomalía para permitir avanzar al lote siguiente. |
| Transacción.VersionNotificada y VersionProcesada | bigint NOT NULL DEFAULT 0 / long con HasDefaultValue(0L). CHECK: 0 ≤ procesada ≤ notificada. |
| NotificacionesPago | ID GUID, FK a transacción, Huella varchar(64), RecibidaUtc datetime2; UNIQUE por transacción/huella. No guarda payloads ni tarjetas. |
| VerificacionesPago | ID GUID estable durante el reintento de la operación, FK a transacción, versión consultada, fecha de procesamiento, estado observado nullable y resultado acotado. Registra incluso respuesta obsoleta/error de comunicación sin alterar estado verificado. |
| FK transacción → intento | (IntentoPagoId,Entorno) → (Id,Entorno), clave alternativa del intento; evita asociación entre entornos desde SQL. IntentoPagoId nullable conserva la bandeja sin compra identificada. |
| Índices | Agendas (Entorno,SiguienteConciliacionUtc) filtradas, FK (IntentoPagoId,Entorno), y verificaciones (TransaccionPagoId,ConsultadaUtc). Conserva las unicidades de referencia, compra, operación, ID Wompi e incidencia. |
| CHECK | Dominios de estados/entorno, confirmación anterior al vencimiento, liberación posterior a creación, versiones no negativas; reflejados en el mapeo EF de las entidades afectadas. |

Los timestamps y estados iniciales de negocio se proporcionan explícitamente por el servicio; no se agregó un default SQL que invente pagos o fechas históricas. Solo los contadores nuevos llevan default cero. Se mantiene importe bigint en centavos y tipos históricos sin cambio.

La generación de DDL de EF en memoria valida el modelo, **no ejecuta ni valida el script preparado contra SQL Server**. Algunas unicidades del script son UNIQUE constraints y EF las representa como índices únicos: equivalentes para protección de datos, no identidad byte a byte de DDL. El script sigue siendo fuente de verdad y no se crearon migraciones.

## 4. Comportamiento de conciliación

1. El controlador exige HTTPS y valida la firma con el secreto de eventos antes de tomar el checksum autenticado como huella. No confía en un estado del navegador.
2. Dentro del bloqueo SQL por entorno/ID, asegura una sola fila TransaccionPago. Si la huella ya existe, no duplica la recepción ni cambia su versión. Una recepción distinta se registra y aumenta VersionNotificada, dejando la agenda lista para consultar.
3. La consulta captura la versión antes de llamar a la API privada. La red sigue fuera de la transacción de inventario.
4. Al persistir el resultado se toma el bloqueo del evento y después el del ID Wompi. Los caminos que solo toman el bloqueo de pago no solicitan luego el de evento. Así se mantiene un orden consistente sin retener bloqueo durante HTTP.
5. Si se recibió un webhook durante la consulta, la respuesta anterior no deja la agenda en NULL: permanece trabajo para la versión nueva. Si ya se procesó una generación superior, se registra la observación antigua sin aplicarla al negocio.
6. APPROVED no se degrada por PENDING/VOIDED/errores posteriores. Un terminal no aprobado no vuelve a PENDING. Una aprobación posterior todavía pasa por el control de la reserva: liberada/expirada no emite ni recupera cupos; conserva incidencia de conciliación.
7. Un error de red o procesamiento reprograma el ID. El último estado verificado no se transforma en rechazo. Se conserva historia con resultado acotado, sin imprimir secretos.
8. Las observaciones posteriores contradictorias de un pago aprobado quedan en historial para revisión; no se ejecuta devolución, revocación ni cambio de inventario automático.

Las consultas privadas desde el navegador aseguran la fila solo después de cotejar referencia, centavos, moneda y compra autorizada. Esa operación interna **no se considera una nueva recepción webhook** y no incrementa artificialmente la generación en cada consulta.

La deduplicación de recepciones protege el mismo ID/huella, no pretende demostrar un ID de evento que Wompi no aporta en este contrato. Dos notificaciones distintas pueden provocar dos consultas legítimas, pero los efectos comerciales se siguen decidiendo bajo bloqueo y estado persistido. No se promete una única solicitud HTTP al proveedor.

## 5. Archivos afectados en esta remediación

- `Sql/UGP_0002_ReservasPagos.sql`.
- `Models/Entities/ReservaCompra.cs`.
- `Data/ReservasMapping.cs`.
- `Services/Implementations/ConciliacionPolicy.cs` (nuevo).
- `Services/Implementations/ReservaTransaccion.cs`.
- `Services/Implementations/ReservasService.cs`.
- `Services/Implementations/ReservasWorker.cs`.
- `Controllers/WompiEventosController.cs`.
- `Program.cs`: únicamente clasificación adicional de errores transitorios en esta remediación.
- `Tests/Reservas/Program.cs`.
- `UGP(ultimo guerrero en pie).csproj`: excluye artifacts de ítems del proyecto para que salidas de pruebas no se incluyan en la aplicación.
- `Documentacion/Auditoria-Previa-Scripts-Inventario-UGP.md`: actualización de estado, conservando el informe y anexos originales como evidencia histórica.
- Este informe (nuevo).

No se cambió el frontend, WompiCheckout, CompraService, cookies, QR, PDF, credenciales, appsettings, baseline ni UGP_0003 durante esta remediación.

## 6. Gobernanza SQL: evidencia del repositorio y propuesta

### Evidencia comprobable sin SQL

- Scripts en `Sql/`: baseline UGP_Database_Schema.sql y propuestas UGP_0002/0003. No hay V0001__baseline.sql ni run.ps1.
- `Data/Migrations/` contiene `20261106182136_InitialCreate.cs`, Designer y snapshot. El baseline contiene DDL y una inserción de esa versión en `__EFMigrationsHistory`, con ProductVersion 10.0.14. **Su existencia en un archivo no prueba ejecución** ni una fila real en el servidor.
- No existe definición contractual de SchemaVersion ni ejecutor de despliegue actual en este repositorio. No se inspeccionó otro proyecto ni se copiaron scripts.
- `appsettings.json` declara la base **UGP_Db_v2**; la factory de diseño incluye ese mismo fallback. No se publicó la cadena de conexión. El host usa configuración estándar y puede recibir overrides de User Secrets/entorno: la base efectiva del proceso en ejecución no se certifica leyendo solo el repositorio.
- Program actual no aplica migraciones ni siembra. No hay evidencia versionada suficiente de cómo se aplicó el esquema real ni qué scripts se ejecutaron. Se requiere comprobación de catálogo y registros de despliegue autorizada en una etapa posterior.
- Reservas:Habilitadas no está declarada en appsettings y es false por defecto. No se cambió. No se consultaron ni alteraron posibles overrides privados.

### Propuesta mínima, NO implementada

1. Primero obtener evidencia de catálogo/servidor/base: objetos, índices, CHECK confiables, __EFMigrationsHistory y existencia/estructura de cualquier tabla de versiones. No crear otra tabla competidora si ya existe una real.
2. Si no existe gobernanza, aprobar un registro `dbo.SchemaVersion` con identificador de script único, SHA-256 de bytes, fecha UTC, ejecutor, ID de despliegue y referencia de aprobación. La fila debe reflejar éxito confirmado, no un intento fallido. Registrar intentos fallidos fuera de la transacción que se revierte.
3. Un manifiesto de entrega debe listar scripts exactos, orden, dependencias y hashes. Conservar el baseline histórico como artefacto inmutable; una constatación de esquema existente no debe falsear que se ejecutó un baseline nuevo.
4. Elegir un único propietario de transacción. El UGP_0002 preparado es propietario y rechaza transacciones externas. Para incorporar registro de versión en su mismo commit, una aprobación posterior debe integrar el INSERT de versión dentro del script o cambiar explícitamente a un contrato controlado por ejecutor. No es correcto añadir una fila SchemaVersion después del commit y llamarlo atómico.
5. El ejecutor futuro debe validar destino/allowlist, hash, permisos, backup y precondiciones; bloquear despliegues concurrentes, cortar ante cualquier error y verificar resultados tras commit incierto. Si la versión ya existe con el mismo hash, comprobar contrato; hash distinto obliga a detenerse. UGP_0002 actualmente rechaza reaplicación en vez de ocultar drift.
6. Probar el contrato y restauración en SQL desechable antes de cualquier base de negocio. Sin aprobación no se crea ni ejecuta ese runner, no se renombra a V0001 y no se manipula __EFMigrationsHistory.

## 7. UGP_0003 y adopción histórica

Permanece congelado. SHA-256: `7A634E2B182ED2062E364FD9FDCA8986F3A531915DC3B846B9E5B76C04352CBC`. Baseline: `A7FE08D90752E1CF438FEDE2D574810368973B42D4284E62E289437D999FB3E1`.

No hay datos reales nuevos que permitan clasificar compras. Plan para una etapa autorizada:

1. Respaldo verificable y ensayo de restauración en otra base; detener escritores antes de adoptar. Conservar referencias externas a pagos aun si se restaura SQL.
2. Inventario de solo lectura de compras sin reserva, estados, detalle/cantidad/monto, cupos globales y de localidad, tickets, huérfanos y duplicados. No partir de que toda compra antigua pendiente sea abandono.
3. Asociar evidencia Wompi verificable y confirmar si ya se descontó o devolvió inventario. Aprobadas y tickets existentes se conservan; no emitir otro lote. Cobros confirmados que UGP aún marca pendientes requieren conciliación individual.
4. Datos ambiguos, fechas sin UTC acreditado, inventario inexplicable o pagos no resueltos quedan bloqueados para revisión. No cambiar su estado por antigüedad ni inventar intentos/referencias observadas.
5. Aprobar manifiesto persistente por compra con origen, estado previo, cantidad efectivamente afectada, evidencia del pago/descuento, actor, fecha y decisión. Registrar adopción como hecho actual, no como un checkout histórico ficticio.
6. Preparar después un script separado corregido, con comparación completa de registros ya adoptados, idempotencia por operación/manifiesto y pruebas de doble liberación y fallo intermedio. No ejecutar el UGP_0003 actual.

## 8. Validación y límites de las pruebas

| Verificación | Resultado |
|---|---|
| `dotnet build --no-restore` a salida habitual | Falló MSB3027/MSB3021: apphost bloqueado por un proceso existente. No se detuvo ese proceso ni se desactivaron protecciones. |
| Compilación web a salida separada | PASA: 0 errores/0 advertencias. |
| Compilación del ejecutable de pruebas a salida separada | PASA: 0 errores/0 advertencias tras alinear restauración local. |
| Pruebas aisladas | **22 PASA**: criptografía, validación de eventos, ID, gate por defecto, reencolado, duplicados, generaciones concurrentes/fuera de orden, preservación de terminales, retry y metadatos/DDL EF en memoria. |
| `git diff --check` | PASA. |
| Revisión de secretos | Escaneo de patrones en los archivos de código/SQL cambiados sin candidatos de claves privadas reales; valores de Tests son FIXTURE_ONLY. Índice vacío: no hay contenido staged. El escaneo no equivale a certificar cualquier formato posible de secreto. |
| UGP_0003 y baseline | Hashes intactos. |
| SQL, rollback/deadlocks/índices reales | **NO EJECUTADO** por restricción explícita. |
| Webhook HTTPS / worker completo / Wompi Sandbox | **NO EJECUTADO**; no se activó el checkout ni se realizaron pagos. |

La primera compilación de pruebas encontró assets de EF 10.0.14 frente a 10.0.15 del web. Se restauraron ambos desde la caché local con reevaluación forzada, sin cambiar PackageReference ni descargar paquetes. NuGetAudit se deshabilitó solo para esa restauración offline; no se afirma auditoría de vulnerabilidades de dependencias.

Comandos de validación reproducibles ya utilizados: `dotnet build --no-restore -o artifacts/remediacion-web`, `dotnet build Tests/Reservas/Reservas.Checks.csproj --no-restore -o artifacts/remediacion-tests`, `dotnet artifacts/remediacion-tests/Reservas.Checks.dll --isolated`. El último declara SQL NO EJECUTADO. No usar `--sql` en esta etapa.

Las pruebas de agenda invocan la política que utiliza el servicio; los índices/FK se comprueban como modelo en memoria. Esto no demuestra serialización real, unicidad aplicada ni entrega efectiva HTTP. El arnés SQL incluye un caso nuevo de ID terminal reencolado, pero no se ejecutó.

## 9. Riesgos residuales y Git

- Sin aprobación del contrato de gobernanza no se puede afirmar compatibilidad SchemaVersion ni autorizar UGP_0002.
- No conocemos anomalías del inventario real, huérfanos, esquema instalado, respaldos ni historial de aplicaciones. UGP_0003 conserva sus bloqueos.
- Los dos registros de trazabilidad requieren acceso restringido y política de retención. Las verificaciones reiteradas pueden aumentar volumen; medir y definir retención antes de producción.
- Retries y exclusión mutua están implementados, no demostrados en SQL. La indisponibilidad total de base no se resuelve con una agenda almacenada en esa misma base.
- Aprobaciones posteriores a liberación siguen necesitando administración/conciliación humana; no se agregó reembolso ni recuperación automática de cupos.
- El ejecutable abierto no fue sustituido ni reiniciado; no se afirma que esté usando esta revisión ni que sus overrides sean los del repositorio.

Rama `main`, HEAD `62bdf1d`, un commit por delante de `origin/main`. Se mantienen todos los cambios preexistentes. Al cierre se verificaron 23 archivos rastreados modificados y 31 sin seguimiento, incluidos los de otras entregas; índice vacío. Sin commit ni push. Los archivos generados de compilación están ignorados y excluidos del proyecto.

**Detenerse para revisión. La siguiente autorización debe especificar gobernanza y entorno SQL desechable; no activar compras ni ejecutar ninguno de los scripts con este informe.**

SHA-256 de UGP_0002 revisión 2, sin ejecutar: `9E801B8D855A9AE5F9AD24A7B0A7B961DE75988213A3CC06324A6E95EC0B6320`. Este hash sustituye al de la propuesta original únicamente para la nueva revisión.
