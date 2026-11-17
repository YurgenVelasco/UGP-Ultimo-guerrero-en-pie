# Auditoría previa de scripts de inventario y reservas — UGP

## Actualización de remediación — 11 de noviembre de 2026

La auditoría y los scripts reproducidos **más abajo se conservan como evidencia de la revisión original**. El archivo UGP_0002 actual fue corregido después de esa revisión; el anexo original de 73 líneas y su hash no deben confundirse con el script vigente. UGP_0003 y el baseline mantienen sus hashes originales. No se ejecutó SQL ni se habilitaron compras.

| Área/hallazgo | Estado actualizado |
|---|---|
| UGP_0002; A-07/A-10/A-12 | Corregidos índices/FK/estados/fechas, dbo explícito, preflight de destino/objetos/inventario y rollback; se agregó soporte de generaciones e historia para pagos. **No autorizado para ejecutar** hasta gobernanza y pruebas SQL. |
| Conciliación; A-04/A-05/A-06/A-11 | Recepciones únicas por huella, reencolado versionado que no pierde notificaciones concurrentes, conservación de terminales, historia de verificaciones y retry por elemento. Bloqueos distinguen errores transitorios. Validación aislada correcta; concurrencia SQL y worker real pendientes. |
| Gobernanza; A-01 | Repositorio declara UGP_Db_v2 y contiene migración histórica/__EFMigrationsHistory; no demuestra qué se aplicó ni define SchemaVersion/runner. Propuesta documentada, sin implementar ejecutor ni tabla. |
| Históricas; A-02/A-03/A-09 | **BLOQUEADO.** UGP_0003 no se modificó; no existen evidencias reales nuevas de compras/pagos/inventario. |
| A-08/A-13 | Preflight y pruebas aisladas ampliadas; registros históricos, restauración, SQL real y HTTPS Sandbox sin verificar. |

Compilaciones con salida separada: 0 errores/0 advertencias. **22 pruebas aisladas PASA**; no equivalen a pruebas en SQL Server. La compilación a la ruta habitual encontró apphost abierto; no se cerró el proceso. Restauración de dependencias de pruebas alineada desde caché local sin cambiar paquetes declarados. `git diff --check` correcto e índice vacío.

El detalle, archivos, condiciones, propuesta de gobernanza y límites están en `Documentacion/Remediacion-Previa-Ejecucion-SQL-UGP.md`. **Ningún dictamen aprueba despliegue, ejecución SQL ni activación del checkout.**

---

## Auditoría original conservada

Fecha: **11 de noviembre de 2026**, America/Bogota. Árbol auditado: `main`, HEAD `62bdf1d`, con cambios locales anteriores conservados.

Raíz de las rutas del informe: `C:\Users\Yurgen Velasco\source\repos\UGP(ultimo guerrero en pie)\UGP(ultimo guerrero en pie)`.

**Dictamen general: NO AUTORIZAR EJECUCIÓN NI ACTIVACIÓN todavía.** UGP_0002 requiere correcciones de despliegue y concordancia de índices; UGP_0003 está bloqueado por su tratamiento y trazabilidad de históricas. También hay defectos del consumidor de pagos que un DDL correcto no resuelve.

Esta auditoría solo lee archivos y crea este informe. No se conectó a SQL Server, no ejecutó scripts, consultas SQL, migraciones, pruebas que escriban datos ni aplicación. No modificó scripts, código, baseline, secretos o configuración. Las conclusiones de concurrencia son análisis estático, **no pruebas reales aprobadas**. La compilación y las diez pruebas aisladas del informe anterior son antecedentes, no se repitieron ni acreditan SQL.

## 1. Inventario real y dictamen individual

| Archivo/ruta desde la raíz | Estado encontrado | Propósito y dictamen |
|---|---|---|
| `Sql/UGP_0002_ReservasPagos.sql` | Nuevo, sin seguimiento; 73 líneas | Crea cuatro tablas y restricciones de reservas/pagos, agrega CHECK de inventario. **REQUIERE CORRECCIÓN**: versión/checksum/ejecutor no definidos, índices incompletos y preflight de históricas pendiente. No contiene DDL destructivo. |
| `Sql/UGP_0003_AdopcionReservasHistoricas.sql` | Nuevo, sin seguimiento; 33 líneas; manifiesto vacío | Inserta reservas vencidas e intentos reconstruidos para compras previas. **BLOQUEADO**: no demuestra que la compra seleccionada pueda liberarse ni conserva evidencia de adopción; inventa la fecha de creación del intento al igualarla a la fecha declarada de compra. |
| `Sql/UGP_Database_Schema.sql` | Baseline real rastreado, sin cambios; 997 líneas según lectura del archivo | DDL histórico generado con control `__EFMigrationsHistory`. **BLOQUEADO como paso automático del despliegue solicitado**: no reemplaza un V0001/SchemaVersion desconocido; no ejecutar sobre una base existente suponiendo compatibilidad. Se conserva íntegro; se puede evaluar como insumo de una base desechable, con aprobación separada. |
| `Sql/V0001__baseline.sql` | **No existe** en este checkout; tampoco apareció en búsqueda de archivos | **BLOQUEADO / no auditable**. No se creó ni se sustituyó por otro archivo. |
| `Sql/run.ps1` | **No existe** en este checkout; tampoco apareció en búsqueda de archivos | **BLOQUEADO / no auditable**. No se puede confirmar descubrimiento de scripts, transacciones, checksum, orden ni corte ante errores de ese ejecutor. |

No hay scripts SQL rastreados modificados según Git. Los dos scripts de reservas son archivos nuevos. No se encontró `SchemaVersion` en los archivos del proyecto revisados. Esto **no demuestra** que no exista en la base real: no se consultó el servidor. Si V0001/run.ps1 pertenecen a otro checkout o no fueron copiados, se necesitan sus versiones reales antes de decidir el mecanismo de despliegue; no mezclar historiales por suposición.

Se inspeccionaron también `Models/Entities/ReservaCompra.cs`, `Data/ReservasMapping.cs`, `Data/ApplicationDbContext.cs`, `CompraService`, `ReservasService`, `ReservaTransaccion`, `ReservasWorker`, `WompiCheckout`, `TicketService`, `PasarelaController`, `WompiEventosController`, `Program.cs`, disponibilidad pública, el arnés de pruebas y `Documentacion/Correccion-Inventario-Reservas-UGP.md`.

## 2. Contenido, tablas, columnas y correspondencia EF

### UGP_0002

| Objeto | Columnas y tipos SQL | Relaciones/restricciones |
|---|---|---|
| `dbo.Eventos` existente | No agrega, cambia ni elimina columnas | `CK_Eventos_Inventario`: capacidad ≥ 0, disponibles ≥ 0 y disponibles ≤ capacidad. `WITH CHECK` valida filas previas y puede bloquear/fallar. |
| `dbo.ReservasCompra` | `CompraId int`; `CreadaUtc`, `ExpiraUtc datetime2`; `Estado nvarchar(20)`; `ConfirmadaUtc`, `LiberadaUtc datetime2 NULL`; `MotivoLiberacion nvarchar(100) NULL`; `OperacionId uniqueidentifier`; `AsistentesJson nvarchar(max) NULL` | PK/FK a Compras, operación única, TTL > 0 y ≤ 15 minutos, estados permitidos, correlación estado/fecha, JSON válido. |
| `dbo.IntentosPago` | `Id uniqueidentifier`; `CompraId int`; `Referencia nvarchar(100)`; `Entorno nvarchar(10)`; `Centavos bigint`; `Moneda nvarchar(3)`; `CreadoUtc datetime2`; `UltimaVerificacionUtc`, `SiguienteConciliacionUtc datetime2 NULL`; `Estado nvarchar(30)` | PK, una fila por compra, FK a Reserva, referencia única por entorno, importe positivo, COP y entornos permitidos. |
| `dbo.TransaccionesPago` | `Id uniqueidentifier`; `IntentoPagoId uniqueidentifier NULL`; `Entorno nvarchar(10)`; `WompiId nvarchar(100) COLLATE Latin1_General_100_BIN2`; `Estado nvarchar(30)`; `RegistradaUtc datetime2`; `VerificadaUtc`, `SiguienteConciliacionUtc datetime2 NULL`; `UltimoResultado nvarchar(40) NULL` | PK, FK opcional al intento, ID Wompi único por entorno. Sin cascada. La bandeja puede existir antes de identificar la compra. |
| `dbo.IncidenciasPago` | `Id uniqueidentifier`; `TransaccionPagoId uniqueidentifier`; `Motivo nvarchar(100)`; `Estado nvarchar(20)`; `CreadaUtc datetime2`; `ResueltaUtc datetime2 NULL`; `ResueltaPor nvarchar(450) NULL`; `EvidenciaResolucion nvarchar(2000) NULL` | PK, una incidencia por transacción, FK, estados limitados, resolución exige fecha, responsable y evidencia no vacíos. No comprueba por sí solo que el responsable esté autorizado. |

Tipos, longitudes y nulabilidad de estas columnas coinciden estáticamente con las entidades y los mapeos: `int`, `long`, `Guid`, `DateTime` y cadenas Unicode; `datetime2` sin precisión explícita corresponde a precisión 7 por defecto. No se detectó conversión decimal/float en el importe del nuevo esquema: son centavos enteros. Las columnas históricas `Compras.monto_total decimal(10,2)` y `fecha_compra datetime` no cambian. El código nuevo trunca la fecha de compra al segundo para no redondearla al día siguiente.

Salvedades de concordancia: los CHECK existen solo en el script, no se describen en EF; varios nombres de PK/FK/UNIQUE los asigna SQL Server automáticamente y no coinciden necesariamente con los nombres convencionales de EF. Esto no impide CRUD, pero invalida asumir igualdad de esquemas generados y exige conservar scripts como fuente de verdad. EF no declara esquema explícito en `ToTable`, mientras el SQL usa `dbo`; comprobar esquema efectivo del usuario para evitar resolución a objetos homónimos. No se inspeccionó el catálogo SQL real.

### Índices

| Índice/restricción | Cobertura | Evaluación |
|---|---|---|
| Reserva PK CompraId y UNIQUE OperacionId | Recuperación de reserva y reconocimiento de reintento de creación | Adecuados por diseño. |
| `IX_ReservasCompra_Estado_ExpiraUtc` | `WHERE Estado='Vigente' AND ExpiraUtc<=... ORDER BY ExpiraUtc` | Adecuado para el worker global; filtro por evento requiere unir Compras. Plan y coste sin medir. |
| Intento UNIQUE CompraId y `(Entorno,Referencia)` | Compra → intento y cotejo de pago | Impiden duplicados relevantes. La referencia generada tiene formato estable; no hay una transacción única por referencia, pues un intento admite varios IDs Wompi. |
| Transacción UNIQUE `(Entorno,WompiId)` | Duplicados de notificación | Adecuado; WompiId distingue mayúsculas por BIN2. |
| `IX_TransaccionesPago_Conciliar(SiguienteConciliacionUtc)` | Selección por vencimiento | Ayuda, pero el worker además filtra Entorno; evaluar `(Entorno,SiguienteConciliacionUtc)` con filtro NOT NULL y cobertura, no añadir a ciegas ambos. |
| Índice de FK `TransaccionesPago.IntentoPagoId` | Anti-join de intentos sin transacción y relaciones | **Ausente en SQL**. EF lo agrega por convención de FK; SQL Server no crea ese índice automáticamente. Corregir concordancia. |
| Índice de `IntentosPago(Entorno,SiguienteConciliacionUtc)` | Barrido de intentos sin ID del worker | **Ausente**. El anti-join y actualización pueden recorrer muchos intentos. Validar propuesta con planes de pruebas. |
| Incidencia UNIQUE TransaccionPagoId | Idempotencia de incidencia | Adecuado; no hay índice por estado/fecha para futura revisión administrativa (endurecimiento, no fallo de compra). |

### UGP_0003 y baseline

UGP_0003 es DML aditivo, no un cambio de esquema: inserta en ReservasCompra e IntentosPago, lee Compras, MetodosPago y Tickets. No modifica inventario de forma directa, pero deja filas `Vigente` ya vencidas que el worker liberará al activarse. **Insertar una fila nueva también puede causar un efecto destructivo de negocio posterior**; “aditivo” no equivale a “seguro para compras históricas”.

El baseline crea las tablas originales, relaciones, índices, secuencia de folios y `__EFMigrationsHistory`. Sus dos `EXEC(N'CREATE UNIQUE INDEX ...')` son textos constantes, no SQL construido desde entradas del comprador. No se identificó inyección SQL en ellos. Hay reglas históricas ON DELETE CASCADE/SET NULL, pero estos scripts nuevos no ejecutan DELETE ni las amplían. El baseline no contiene datos reales de clientes ni contraseñas incrustadas observadas; `PasswordHash` es una definición de columna de Identity.

## 3. Hallazgos priorizados

Confirmado significa demostrado en el texto del código/script, no reproducido contra un servidor.

| ID / severidad | Archivo y línea | Evidencia y efecto | Corrección mínima recomendada, NO aplicada |
|---|---|---|---|
| A-01 Alta, confirmado | `Sql/UGP_0002_ReservasPagos.sql:5`; `Sql/UGP_0003_AdopcionReservasHistoricas.sql:6`; archivos ausentes | Ningún script consulta/escribe SchemaVersion, ni registra checksum. No hay run.ps1. El único control histórico es __EFMigrationsHistory del baseline. No hay compatibilidad de versionado demostrada. | Obtener contrato real del ejecutor/SchemaVersion. Registrar versión, checksum, ejecutor y fecha en la misma transacción del cambio; definir un solo propietario de transacción y fallo inmediato. No inventar el esquema de esa tabla. |
| A-02 Alta, confirmado | `Sql/UGP_0003_AdopcionReservasHistoricas.sql:21` y `:25` | `NEWID()` crea intento y `CreadoUtc=h.CreadaUtc` atribuye al intento una fecha de compra declarada; referencia y COP se reconstruyen, sin evidencia del checkout histórico. No se conserva origen, manifiesto, aprobador ni instante de adopción. | Registrar adopción como hecho nuevo, con fecha actual, procedencia y evidencia; no etiquetar un intento histórico como observado sin prueba. Separar “referencia reconstruida” de “referencia enviada/verificada”. |
| A-03 Alta, confirmado | `Sql/UGP_0003_AdopcionReservasHistoricas.sql:14` a `:23` | Solo exige Pendiente, Wompi, antigüedad declarada y ausencia de tickets. Un pago histórico aprobado pero aún no reflejado cumple esas condiciones. El worker liberará sus cupos. El comentario “conciliación previa” no es una precondición persistida verificable. | Manifiesto persistente y aprobado por compra con evidencia del pago y del descuento original. Excluir aprobadas, no resueltas o válidas sin autorización específica. No aplicar retroactivamente el TTL nuevo como prueba de abandono. |
| A-04 Alta, confirmado | `Services/Implementations/ReservasService.cs:58` y `:120`; `ReservasWorker.cs:30` | Recepción de un ID existente no cambia agenda. Tras un terminal `SiguienteConciliacionUtc=null`, otro transaction.updated del mismo ID recibe 200 pero no reencola consulta. Un estado posterior relevante puede no conciliarse automáticamente. | Marcar cada recepción auténtica como trabajo pendiente con generación/versión persistente, preservando idempotencia de efectos y monotonicidad. Probar que una verificación en curso no borre la marca de un evento recién llegado. |
| A-05 Alta, escenario derivado del código | `ReservasWorker.cs:33` y `:44`; `ReservasService.cs:19` a `:26` | Una excepción persistente de la primera transacción interrumpe todo el lote y repite el mismo orden. En expiración, 100 reservas fallidas más antiguas ocupan siempre el TOP 100. Puede haber hambre de cola y cupos bloqueados indefinidamente. | Manejo por elemento, reintento persistente con backoff, siguiente selección justa y cola/incidencia operativa; una anomalía no debe impedir procesar las demás. Probar con un elemento fallido y con 100. |
| A-06 Media, confirmado | `Services/Implementations/ReservaTransaccion.cs:14`; `ReservasService.cs:55`; `Program.cs:21` | Todos los retornos negativos de applock pasan a 51001, incluido -3. `EnableRetryOnFailure` no configura ese número. No equivale a preservar un error 1205 reintentable; puede perderse el reintento automático del bloqueo. | Distinguir timeout, cancelación, deadlock y parámetros. Rollback completo y política acotada para errores transitorios concretos; no reintentar indiscriminadamente cualquier 51001. |
| A-07 Media, confirmado | `Sql/UGP_0002_ReservasPagos.sql:48` y `:58`; `Data/ReservasMapping.cs:42`; `ReservasWorker.cs:36` | Índice de FK no reflejado en SQL y búsqueda de intentos pendientes sin índice de agenda. Riesgo de barridos, bloqueos y retraso de recuperación. | Ajustar índices del script y mapeo, luego medir con volumen y planes en pruebas. |
| A-08 Alta para despliegue, comprobación pendiente | `Sql/UGP_0002_ReservasPagos.sql:11` | WITH CHECK puede fallar por saldos históricos negativos/superiores al aforo. No hay evidencia de calidad de datos ni prueba de restauración. No se puede certificar compatibilidad histórica. | Preflight de solo lectura aprobado, respaldos restaurables y revisión de anomalías, sin clamping/recalcular inventario a ciegas. |
| A-09 Media, confirmado | `Sql/UGP_0003_AdopcionReservasHistoricas.sql:23` y `:31` | NOT EXISTS omite filas ya presentes sin cotejar fecha, entorno, referencia, importe ni estado. Una repetición con manifiesto distinto puede pasar sin aplicar lo pedido; una adopción parcial puede enlazar información incompatible. | Comparación completa de filas previas; conflicto explícito si difieren. Clave/hash del manifiesto y resultado por compra. Exigir operación previa consistente, no solo ausencia de filas. |
| A-10 Media, confirmado | `Sql/UGP_0002_ReservasPagos.sql:5` a `:8`; baseline `:1` y `:11` | UGP_0002 solo comprueba existencia de una tabla. No valida destino, esquema base, otros objetos parciales, sesión con transacción externa o versión SQL. XACT_ABORT no cubre todos los errores de compilación. Baseline crea la tabla de historial fuera de la transacción principal y no declara XACT_ABORT. | Preflight y ejecutor que pare ante error, conexión dedicada y control TRY/CATCH/XACT_STATE según quién sea dueño de la transacción. No modificar baseline; añadir envoltura externa revisada si procede. |
| A-11 Media, confirmado | `ReservasWorker.cs:38` a `:40`; `ReservasService.cs:109` a `:120` | UltimaVerificacionUtc se actualiza incluso para un intento sin ID consultable; los estados de pago se sobrescriben y no hay historial de recepción/decisiones. Puede confundirse revisión de agenda con verificación de proveedor. | Separar fecha de intento de consulta, respuesta válida y decisión; conservar procedencia y cambios relevantes. No guardar payloads de tarjetas. |
| A-12 Media, riesgo de consistencia externa | `Sql/UGP_0002_ReservasPagos.sql:48` a `:56`; `ReservasMapping.cs:42` | FK solo por IntentoPagoId: SQL permite Entorno distinto al intento; Estado de intento/transacción no tiene dominio CHECK. Fechas confirmada/liberada no están acotadas respecto de creación. El servicio restringe varias de estas condiciones, no la base. | Endurecer invariantes que deban sobrevivir a otros escritores. No se identificó una vía pública que permita inyectar esas filas. |
| A-13 Media, limitación conocida | `Tests/Reservas/Program.cs:175` en adelante | Pruebas llaman VerificarAsync directamente tras Notify y no demuestran reencolado por el worker; el fallo inyectado es una excepción de interceptor, no un deadlock SQL ni commit incierto. Procesos de servicio no prueban reinicio del host completo. | Añadir pruebas del consumidor real, recepción durante verificación, error 1205/applock, caída después de commit y arranque sin navegador. No atribuirles cobertura que no tienen. |

### Reproducción conceptual de A-04, sin ejecutar

1. Registrar un webhook auténtico de T; el worker consulta un estado terminal y pone su agenda en NULL.
2. Llegar otra notificación auténtica de T con información posterior que requiera revisión.
3. `AnyAsync` devuelve true: no se actualiza la fila, ni se conserva la nueva recepción. El controlador responde 200.
4. El worker solo selecciona agendas vencidas no nulas; T no vuelve a consultarse. Una llamada manual desde el navegador podría consultar, pero no satisface recuperación sin navegador.

El defecto está en la entrega de trabajo, aunque los cambios de inventario sean idempotentes. No implica que Wompi cambie necesariamente un DECLINED a APPROVED; cualquier revisión posterior auténtica de un ID terminal queda sujeta al mismo comportamiento. Además APPROVED es absorbente en el consumidor: un VOIDED posterior no revierte cupos (prudente), pero tampoco crea una alerta específica; debe revisarse su política comercial sin simular reembolsos.

## 4. Atomicidad, bloqueos y carreras

| Operación | Protección observada | Límite de la conclusión |
|---|---|---|
| Nueva reserva | `CompraService.cs:59` SERIALIZABLE; `:63` applock por evento; valida localidad/cupo; guarda compra, detalle, reserva, intento y descuento; commit en `:172` | Correcto estructuralmente para escritores que usan este protocolo. El descuento es una modificación de entidad dentro del bloqueo, no un UPDATE condicional autónomo. Escritores antiguos/SQL manual no participan del applock. |
| Último cupo | Saldo global y saldo de localidad leídos después de tomar el bloqueo del evento | Prevención por diseño; falta prueba con dos conexiones/procesos y SQL real. |
| Liberación | `ReservasService.cs:168` transacción; applock; reread; `LiberarAsync` solo Vigente/no Aprobado; UPDATE condicionado a límites; estado y fecha en el mismo commit | Repetir no devuelve otra vez si la primera transición quedó confirmada. Falta prueba de fallo/commit ambiguo. |
| Confirmación vs expiración | Ambas usan el mismo bloqueo y releen reserva; se toma reloj SQL después del bloqueo. Aprobación fuera del plazo libera y crea incidencia | Una sola transición efectiva por diseño. Una compra confirmada conserva cupos. No se descuenta al emitir. |
| Tickets | GenerarTickets usa el mismo DbContext y transacción, incluso la secuencia (`TicketService.cs:31`); SaveChanges no hace commit independiente de la transacción exterior | No emitir dos veces depende de la reserva/estado y de no usar otros emisores. UNIQUE codigo_unico/QR no impide emitir un segundo lote con nuevos folios. Huecos de secuencia por rollback no equivalen a duplicados. |
| Webhook duplicado | Applock por entorno/ID e índice único; una sola fila | Evita filas duplicadas, pero no garantiza procesamiento de actualizaciones posteriores (A-04). |
| Pago tardío | Reserva liberada no se reactiva; incidencia UNIQUE por transacción; no emisión | Correcto por rama revisada si se llega a consultar la aprobación; A-04/A-05 pueden impedir llegar a ella. |
| Reintento de creación | OperacionId se crea fuera del delegado EF y se consulta dentro de cada intento | Cubre reintentos de esa invocación. No es una clave de idempotencia durable de la petición del comprador: otra invocación genera otra operación. No afirmar deduplicación de todos los reintentos HTTP. |

Orden observado: creación toma bloqueo de evento antes de leer/escribir inventario; confirmación y expiración también. Registrar notificación usa bloqueo de ID en una transacción separada que termina antes de tomar el bloqueo de evento: no se observa inversión anidada entre esos dos applocks. Las escrituras de agenda fuera de ese bloqueo y las tablas compartidas Visitantes/MetodosPago pueden competir; no se puede garantizar ausencia de deadlocks. Dos eventos distintos tampoco comparten applock y pueden tocar el mismo visitante. La adopción usa UPDLOCK/HOLDLOCK sobre compras, **no** el protocolo de evento: la aplicación detenida es una precondición real, no opcional.

EF reejecuta el delegado transaccional ante fallos reconocidos; el código limpia ChangeTracker. En creación, el `catch` intenta Rollback y podría sustituir la excepción original si el rollback falla tras perder la conexión: incluir ese caso en pruebas. El proveedor reconoce 1205, pero no el 51001 definido por UGP en el detector consultado. Fuentes: [resiliencia EF](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency), [detector EF 10.0.14](https://github.com/dotnet/efcore/blob/v10.0.14/src/EFCore.SqlServer/Storage/Internal/SqlServerTransientExceptionDetector.cs).

Un applock propiedad Transaction se libera al terminarla; su retorno -3 requiere tratamiento explícito. La transacción y el bloqueo protegen atomicidad, no prueban por sí solos progreso de las colas. [Documentación de sp_getapplock](https://learn.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-getapplock-transact-sql?view=sql-server-ver17).

## 5. Históricas: identificación y adopción propuesta

**No se identificaron IDs ni cantidades reales**, pues no se consultó la base. En el esquema anterior, toda compra sin fila ReservaCompra es candidata a clasificación histórica; no basta una fecha de corte. Antes de aplicar UGP_0002 la tabla todavía no existe. No ejecutar un LEFT JOIN a una tabla inexistente para hacer el inventario.

Preflight posterior autorizado: inventariar por estado original, método, evento, cantidad/suma de detalles, monto, existencia de tickets y reserva; proteger datos personales. Verificar existencia de tablas antes de elegir la consulta. Identificar pendientes sin reserva, aprobadas con/sin tickets, rechazadas/canceladas que puedan seguir descontadas, estados nulos/desconocidos e inconsistencias globales y de localidad. El script actual únicamente adopta una parte de las pendientes; no resuelve todos los descuentos históricos.

| Situación verificada | Tratamiento propuesto, no ejecutado |
|---|---|
| Aprobada y tickets existentes | Conservar estados, cupos y boletos. Registrar clasificación/conciliación si se requiere, sin volver a emitir. |
| Pendiente en UGP, cobro aprobado confirmado | No adoptar como vencida automáticamente. Conciliación individual que preserve derechos/inventario; comprobar tickets antes de cualquier reparación. |
| Pendiente y resultado externo desconocido | Mantener como histórica por revisar; no inventar intento ni declarar rechazo. D-02 de reservas nuevas no prueba que esa compra antigua fuera una reserva de 15 minutos. |
| Rechazo/anulación definitiva con evidencia y descuento original comprobado | Preparar liberación aprobada individual, transaccional, con cantidad verificada, marca única y trazabilidad de quién decide. |
| Abandono comprobado y liberación autorizada | Adoptar como reserva histórica con origen explícito, sin fabricar hechos de pago. Guardar manifiesto y operación estable; luego liberar una vez por el servicio validado. |
| Fechas/importe/inventario dudosos o tickets inconsistentes | Bloquear esa adopción, conservar datos y producir incidencia. No corregir con sumas globales ni limitar saldos arbitrariamente a capacidad. |

El manifiesto debe incluir ID de compra, evidencia y fuente de fecha UTC (sin deducirla de `datetime` local), estado original, cantidad efectivamente descontada, referencia original si consta, transacciones verificadas si existen, decisión, aprobador, fecha de adopción y hash/ID del lote. Si la referencia solo se puede reconstruir, etiquetarla como reconstruida; el valor calculado no prueba que se haya enviado a Wompi. Una fecha nula no debe transformarse en 1970 como hecho histórico, aunque ese fallback replique el código actual.

Antes de aceptar un reintento del manifiesto, comparar todos los valores ya adoptados; si difieren, detenerse. El manifiesto en variable de tabla de UGP_0003 desaparece al terminar: hace falta evidencia durable. Esta propuesta requiere una corrección posterior aprobada, no se agregó una tabla ni columnas durante la auditoría.

## 6. Seguridad, versión y activación

- No se observaron credenciales en los tres SQL. Los servicios leen llaves desde IConfiguration; no se inspeccionaron ni publicaron User Secrets. Los valores del arnés son fixtures explícitos, no credenciales operativas.
- Las llamadas `ExecuteSqlInterpolatedAsync` pasan el recurso de applock como parámetro; no concatenan entradas del comprador dentro de una consulta. No se identificó una ruta de inyección SQL en el flujo auditado.
- Program no llama Migrate/EnsureCreated ni siembra al arrancar. El baseline sí tiene metadatos de una migración histórica: no es equivalente a ejecutar EF, pero tampoco constituye SchemaVersion.
- El baseline controla objetos con una sola fila de __EFMigrationsHistory. Si esa fila existe puede omitir DDL sin detectar drift; si no existe pero las tablas sí, puede fallar por duplicidad. No usarlo como herramienta de reparación sobre la base real ni editarlo para adaptar una base desconocida.
- La compatibilidad SchemaVersion está **no demostrada**. Un ejecutor que use patrón `V*.sql` podría ni siquiera descubrir UGP_0002/0003; es un ejemplo condicionado al ejecutor ausente, no un hecho observado. No renombrar hasta revisar el contrato real.
- Reservas:Habilitadas es false por defecto y gatea worker, creación, pasarela y consulta de pago en las rutas revisadas. No se cambió ese valor. **No se certificó la configuración efectiva de un proceso en ejecución**, que podría tener overrides privados; se debe verificar sin imprimir secretos antes del despliegue.
- No hay gate que certifique automáticamente versión/checksum y pruebas aprobadas antes de activar. Añadir preflight de versión compatible en la futura implementación, con fallo cerrado. El secreto de eventos y HTTPS no prueban por sí solos que el esquema esté listo.
- Los scripts no tienen validación explícita del nombre/servidor destino. Ejecutarlos en la conexión equivocada es posible; el preflight y el ejecutor deben validar allowlist de base de pruebas, identidad y permisos.

## 7. Plan de aplicación en SQL Server de pruebas — NO ejecutado

1. Resolver A-01: aportar baseline y ejecutor reales o aprobar el contrato de despliegue que los sustituya, sin modificar el baseline conservado. Determinar SchemaVersion real, hashes, orden y propiedad de transacción. Corregir A-02/A-03/A-04 antes de habilitar el flujo.
2. Seleccionar una instancia/base desechable `UGP_Reservas_Test_*`, credenciales separadas y sin datos productivos. Obtener autorización expresa para crear/aplicar/insertar pruebas. No usar la conexión de UGP por defecto. Preparar casos históricos sintéticos, nunca copiar PII sin tratamiento aprobado.
3. Capturar catálogo real, versión y compatibilidad SQL, esquema `dbo`, constraints/FK/índices, filas y calidad de datos. Confirmar soporte de ISJSON y permisos de applock. Revisar tiempos de bloqueo y espacio de log.
4. Preparar restauración de referencia; probar baseline solo en base vacía destinada a pruebas y bajo ejecutor revisado. No lanzar comandos EF.
5. Aplicar el script corregido de esquema con registro de versión/checksum, salida de error no cero y sin continuar al siguiente archivo tras fallar. Validar catálogo contra el modelo: todas las columnas, índices y CHECK habilitados/confiables.
6. Probar CHECK de inventario con datos inconsistentes en otra base sintética: debe fallar sin objetos parciales ni versión registrada. Reaplicación y fallo intermedio deben dar un resultado verificable; no asumir que toda excepción equivale a rollback completo.
7. Probar adopción corregida con manifiestos válidos, vacíos, contradictorios, repetidos, parciales, fechas nulas, cantidades no positivas, precio/suma incoherente y compras con pago aprobado no reflejado. Ninguna clasificación dudosa debe terminar liberada por defecto.
8. Ejecutar el arnés SQL existente después de revisar sus limitaciones. Ampliar con dos procesos reales, límite de cupo global/localidad, expiración y aprobación simultáneas, recepción de webhook durante verificación, reencolado de ID terminal, lotes con errores persistentes, deadlock, timeout de applock y pérdida de conexión tras commit.
9. Comprobar invariantes tras cada caso: disponibles en [0,aforo], diferencia exacta respecto del saldo inicial, liberación única, compra/reserva coherentes, número de tickets igual a cantidad autorizada, incidencia única y sin emisión para reservas liberadas. No exigir folios consecutivos tras rollback.
10. Probar reinicio completo del host con reservas vencidas y transacciones pendientes, sin abrir navegador. Después, webhook HTTPS y checkout Sandbox reales. Separar resultados SQL con proveedor fixture de pruebas reales Wompi. No habilitar producción con las primeras únicamente.

El arnés actual ejecuta inserciones y necesita esquema aplicado: **no se ejecutó**. Su guardia de nombre de base no reemplaza autorización ni revisión del servidor destino.

## 8. Respaldo y recuperación ante fallos

No se encontró procedimiento versionado de respaldo/restauración en Sql o en la documentación revisada. No se verificaron backups existentes; no se tomó ninguno.

Plan propuesto para aprobar con DBA:

- Antes de tocar una base no desechable: respaldo completo COPY_ONLY con CHECKSUM cuando corresponda a la plataforma/estrategia existente; destino protegido, capacidad, permisos y material criptográfico de restauración comprobados. Registrar hora, base, LSN, ubicación protegida y resultado sin poner credenciales en Git. COPY_ONLY no cambia la base diferencial habitual. [Microsoft: copy-only](https://learn.microsoft.com/en-us/sql/relational-databases/backup-restore/copy-only-backups-sql-server?view=sql-server-ver17).
- Validar legibilidad/checksum y **restaurar en otra base** para comprobar integridad y comportamiento; VERIFYONLY no verifica toda la estructura de los datos ni sustituye ese ensayo. [Microsoft: VERIFYONLY](https://learn.microsoft.com/en-us/sql/t-sql/statements/restore-statements-verifyonly-transact-sql?view=sql-server-ver17).
- Detener todos los escritores/instancias antiguas durante la intervención; conservar binario/configuración anterior y la nueva versión, pero no reactivar binarios antiguos que desconozcan reservas ya creadas.
- Si falla antes de commit: cortar el ejecutor, revisar conexión/XACT_STATE/@@TRANCOUNT y confirmar rollback por el responsable de la transacción. XACT_ABORT cubre errores de ejecución, no todos los de compilación. No continuar ni “marcar aplicada” una versión fallida. [Microsoft: XACT_ABORT](https://learn.microsoft.com/en-us/sql/t-sql/statements/set-xact-abort-transact-sql?view=sql-server-ver17).
- Tras pérdida de conexión durante commit: inspeccionar checksum/versión y objetos/filas exactas antes de reintentar. Existencia de ReservasCompra no prueba aplicación completa. Una conexión nueva no puede simplemente deshacer un commit anterior.
- Si esquema ya está confirmado pero no hubo tráfico nuevo: mantener mantenimiento, preferir corrección aditiva revisada; restaurar referencia solo con autorización y destino verificado. No hacer DROP automático de las tablas nuevas.
- Si hubo reservas, tickets o pagos desde el respaldo: no restaurar sobre ellos automáticamente, porque se perderían hechos financieros y podría repetirse emisión. Conservar estado y evidencia, detener nuevas ventas, restaurar en otra base para comparar y conciliar con Wompi. Las devoluciones requieren evidencia externa; una restauración SQL no devuelve dinero.
- UGP_0003 ya aplicado con worker activado requiere analizar cada liberación/cobro. Borrar filas de adopción o sumar/restar aforo como “rollback” puede duplicar efectos. Usar corrección compensatoria aprobada y auditable, no eliminación.

## 9. Condiciones de aprobación y alcance final

Orden de corrección recomendado: (1) contrato de SchemaVersion/ejecutor y destino; (2) adopción histórica con evidencia; (3) reencolado y progreso del worker; (4) errores de applock y recuperación; (5) concordancia de índices/endurecimiento; (6) SQL real, recuperación y Sandbox.

Los CHECK/UNIQUE y el protocolo de bloqueo aportan una base razonable. **No prueban aún “exactamente una vez” de extremo a extremo**, especialmente entrega de notificaciones, procesamiento eventual y adopción histórica. No afirmar sobreventa observada: la carrera de inventario está protegida en las rutas revisadas; lo pendiente es demostrarla en SQL y corregir los fallos concretos de progreso y despliegue.

Único archivo creado por esta auditoría: este informe. No se editaron scripts ni código. Estado inicial: 23 archivos rastreados modificados y 28 sin seguimiento, índice vacío, main un commit por delante de origin/main. Estado esperado al cierre: los mismos cambios más este informe (29 sin seguimiento). Sin commits, push, cambios SQL ni activación.

## 10. Scripts completos auditados

Las copias siguientes son evidencia para revisión, **no instrucciones para ejecutar**. Se incluyen los dos scripts nuevos y el baseline real completo, sin sustituir los archivos ausentes. Hashes SHA-256 de los archivos originales:

- UGP_0002_ReservasPagos.sql: `BC741D75F9896BD3BE438D5A4069679353B67BFD539B9E3950421E6FB819D1AB`.
- UGP_0003_AdopcionReservasHistoricas.sql: `7A634E2B182ED2062E364FD9FDCA8986F3A531915DC3B846B9E5B76C04352CBC`.
- UGP_Database_Schema.sql: `A7FE08D90752E1CF438FEDE2D574810368973B42D4284E62E289437D999FB3E1`.

El baseline conserva además el mismo objeto Git que HEAD: `0b1af857cf8480e059b340a1ffbf38381817d568`. Los hashes identifican esta revisión y deben recalcularse cuando se autoricen correcciones.


### Sql/UGP_0002_ReservasPagos.sql

```sql
-- PROPUESTA PARA REVISION. NO ejecutado por la aplicación.
-- Aplicar exclusivamente tras aprobación, primero en una base SQL desechable.
-- El único baseline existente es UGP_Database_Schema.sql; permanece intacto.
-- No migra compras históricas: requieren conciliación y adopción explícita.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.ReservasCompra', N'U') IS NOT NULL
    THROW 51000, 'UGP_0002 ya existe. Revisar versión antes de ejecutar.', 1;

-- Falla sin corregir datos si el inventario existente ya es inconsistente.
ALTER TABLE dbo.Eventos WITH CHECK ADD CONSTRAINT CK_Eventos_Inventario
    CHECK (capacidad_maxima >= 0 AND tickets_disponibles >= 0 AND tickets_disponibles <= capacidad_maxima);

CREATE TABLE dbo.ReservasCompra (
    CompraId int NOT NULL PRIMARY KEY REFERENCES dbo.Compras(compra_id),
    CreadaUtc datetime2 NOT NULL,
    ExpiraUtc datetime2 NOT NULL,
    Estado nvarchar(20) NOT NULL,
    ConfirmadaUtc datetime2 NULL,
    LiberadaUtc datetime2 NULL,
    MotivoLiberacion nvarchar(100) NULL,
    OperacionId uniqueidentifier NOT NULL UNIQUE,
    AsistentesJson nvarchar(max) NULL,
    CONSTRAINT CK_Reserva_TTL CHECK (ExpiraUtc > CreadaUtc AND ExpiraUtc <= DATEADD(minute,15,CreadaUtc)),
    CONSTRAINT CK_Reserva_Estado CHECK (Estado IN ('Vigente','Confirmada','Liberada')),
    CONSTRAINT CK_Reserva_Confirmacion CHECK ((Estado='Confirmada' AND ConfirmadaUtc IS NOT NULL) OR (Estado<>'Confirmada' AND ConfirmadaUtc IS NULL)),
    CONSTRAINT CK_Reserva_Liberacion CHECK (
        (Estado = 'Liberada' AND LiberadaUtc IS NOT NULL AND MotivoLiberacion IS NOT NULL) OR
        (Estado <> 'Liberada' AND LiberadaUtc IS NULL AND MotivoLiberacion IS NULL)),
    CONSTRAINT CK_Reserva_Asistentes CHECK (AsistentesJson IS NULL OR ISJSON(AsistentesJson)=1)
);
CREATE INDEX IX_ReservasCompra_Estado_ExpiraUtc ON dbo.ReservasCompra(Estado,ExpiraUtc);
CREATE TABLE dbo.IntentosPago (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    CompraId int NOT NULL UNIQUE REFERENCES dbo.ReservasCompra(CompraId),
    Referencia nvarchar(100) NOT NULL,
    Entorno nvarchar(10) NOT NULL CHECK (Entorno IN ('Sandbox','Production')),
    Centavos bigint NOT NULL CHECK (Centavos > 0),
    Moneda nvarchar(3) NOT NULL CHECK (Moneda='COP'),
    CreadoUtc datetime2 NOT NULL,
    UltimaVerificacionUtc datetime2 NULL,
    SiguienteConciliacionUtc datetime2 NULL,
    Estado nvarchar(30) NOT NULL,
    CONSTRAINT UQ_IntentosPago_Referencia UNIQUE(Entorno,Referencia)
);
CREATE TABLE dbo.TransaccionesPago (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    IntentoPagoId uniqueidentifier NULL REFERENCES dbo.IntentosPago(Id),
    Entorno nvarchar(10) NOT NULL,
    WompiId nvarchar(100) COLLATE Latin1_General_100_BIN2 NOT NULL,
    Estado nvarchar(30) NOT NULL,
    RegistradaUtc datetime2 NOT NULL,
    VerificadaUtc datetime2 NULL,
    SiguienteConciliacionUtc datetime2 NULL,
    UltimoResultado nvarchar(40) NULL,
    CONSTRAINT UQ_TransaccionesPago_Wompi UNIQUE(Entorno,WompiId)
);
CREATE INDEX IX_TransaccionesPago_Conciliar ON dbo.TransaccionesPago(SiguienteConciliacionUtc);
CREATE TABLE dbo.IncidenciasPago (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    TransaccionPagoId uniqueidentifier NOT NULL UNIQUE REFERENCES dbo.TransaccionesPago(Id),
    Motivo nvarchar(100) NOT NULL,
    Estado nvarchar(20) NOT NULL CHECK (Estado IN ('Pendiente','EnRevision','Resuelta')),
    CreadaUtc datetime2 NOT NULL,
    ResueltaUtc datetime2 NULL,
    ResueltaPor nvarchar(450) NULL,
    EvidenciaResolucion nvarchar(2000) NULL,
    CONSTRAINT CK_Incidencia_Resolucion CHECK (
        (Estado='Resuelta' AND ResueltaUtc IS NOT NULL AND ResueltaPor IS NOT NULL AND LEN(LTRIM(RTRIM(ResueltaPor))) > 0
            AND EvidenciaResolucion IS NOT NULL AND LEN(LTRIM(RTRIM(EvidenciaResolucion))) > 0) OR
        (Estado<>'Resuelta' AND ResueltaUtc IS NULL))
);
COMMIT;
```


### Sql/UGP_0003_AdopcionReservasHistoricas.sql

```sql
-- PLANTILLA DE DATOS PARA REVISION SEPARADA. NO EJECUTADA.
-- Requiere UGP_0002, aplicación detenida y conciliación de las compras seleccionadas.
-- No inferir UTC desde fecha_compra: el operador debe confirmar cada instante.
-- Solo adopta compras ya vencidas; el worker las liberará bajo D-02.
-- Un cobro aprobado descubierto después pasa a conciliación, NO emite tickets.
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
DECLARE @Historicas TABLE (CompraId int PRIMARY KEY, CreadaUtc datetime2 NOT NULL, Entorno nvarchar(10) NOT NULL);
-- Completar EXCLUSIVAMENTE con IDs/fechas verificados y aprobados; no hay valores de ejemplo ejecutables.
-- INSERT INTO @Historicas(CompraId,CreadaUtc,Entorno) VALUES (...);
IF NOT EXISTS (SELECT 1 FROM @Historicas)
    THROW 51002, 'Se requiere un manifiesto de compras históricas revisado.', 1;
BEGIN TRANSACTION;
IF EXISTS (SELECT 1 FROM @Historicas h LEFT JOIN dbo.Compras c WITH(UPDLOCK,HOLDLOCK) ON c.compra_id=h.CompraId
    LEFT JOIN dbo.MetodosPago m ON m.metodo_pago_id=c.metodo_pago_id
    WHERE c.compra_id IS NULL OR c.estado_pago IS NULL OR c.estado_pago<>'Pendiente' OR m.nombre IS NULL OR m.nombre<>'Wompi'
        OR h.Entorno NOT IN ('Sandbox','Production') OR DATEADD(minute,15,h.CreadaUtc)>SYSUTCDATETIME()
        OR EXISTS (SELECT 1 FROM dbo.Tickets t WHERE t.compra_id=c.compra_id))
    THROW 51003, 'Manifiesto incompatible: revisar estados, fechas, método y tickets.', 1;

INSERT INTO dbo.ReservasCompra(CompraId,CreadaUtc,ExpiraUtc,Estado,OperacionId)
SELECT h.CompraId,h.CreadaUtc,DATEADD(minute,15,h.CreadaUtc),'Vigente',NEWID()
FROM @Historicas h WHERE NOT EXISTS (SELECT 1 FROM dbo.ReservasCompra r WHERE r.CompraId=h.CompraId);

INSERT INTO dbo.IntentosPago(Id,CompraId,Referencia,Entorno,Centavos,Moneda,CreadoUtc,SiguienteConciliacionUtc,Estado)
SELECT NEWID(),h.CompraId,
    CONCAT('UGP-TX-',CONVERT(char(8),ISNULL(c.fecha_compra,CONVERT(datetime,'19700101')),112),'-',
        REPLICATE('0',CASE WHEN LEN(CONVERT(varchar(10),h.CompraId))<6 THEN 6-LEN(CONVERT(varchar(10),h.CompraId)) ELSE 0 END),h.CompraId),
    h.Entorno,CONVERT(bigint,c.monto_total*100),'COP',h.CreadaUtc,SYSUTCDATETIME(),'PorConciliar'
FROM @Historicas h JOIN dbo.Compras c ON c.compra_id=h.CompraId
WHERE NOT EXISTS (SELECT 1 FROM dbo.IntentosPago i WHERE i.CompraId=h.CompraId);
COMMIT;
-- No modifica inventario directamente. No ejecutar el worker hasta aprobar el manifiesto.
```


### Sql/UGP_Database_Schema.sql

```sql
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE SEQUENCE [seq_ticket_codigo] START WITH 1000 INCREMENT BY 1 NO CYCLE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AIConfiguracion] (
        [ai_configuracion_id] int NOT NULL IDENTITY,
        [chat_activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [mostrar_boton] bit NOT NULL DEFAULT CAST(1 AS bit),
        [nombre_asistente] nvarchar(100) NOT NULL,
        [mensaje_bienvenida] nvarchar(500) NOT NULL,
        [mensaje_despedida] nvarchar(500) NOT NULL,
        [avatar_url] varchar(500) NULL,
        [mensaje_bloqueo] nvarchar(500) NOT NULL,
        [prompt_sistema] nvarchar(max) NOT NULL,
        [tono] varchar(50) NOT NULL,
        [longitud_maxima] int NOT NULL DEFAULT 500,
        [formalidad] varchar(50) NOT NULL,
        [ollama_url] varchar(500) NOT NULL,
        [modelo] varchar(100) NOT NULL,
        [temperatura] decimal(3,2) NOT NULL DEFAULT 0.7,
        [max_tokens] int NOT NULL DEFAULT 2048,
        [timeout_segundos] int NOT NULL DEFAULT 120,
        [fecha_creacion] datetime2 NOT NULL DEFAULT ((getdate())),
        [fecha_modificacion] datetime2 NOT NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_AIConfiguracion] PRIMARY KEY ([ai_configuracion_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AIContextoConfig] (
        [ai_contexto_config_id] int NOT NULL IDENTITY,
        [clave] varchar(100) NOT NULL,
        [nombre] nvarchar(200) NOT NULL,
        [descripcion] nvarchar(500) NULL,
        [activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [orden] int NOT NULL DEFAULT 0,
        [fecha_creacion] datetime2 NOT NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_AIContextoConfig] PRIMARY KEY ([ai_contexto_config_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AIPalabraRestringida] (
        [ai_palabra_restringida_id] int NOT NULL IDENTITY,
        [palabra] nvarchar(200) NOT NULL,
        [activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [fecha_creacion] datetime2 NOT NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_AIPalabraRestringida] PRIMARY KEY ([ai_palabra_restringida_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AITema] (
        [ai_tema_id] int NOT NULL IDENTITY,
        [nombre] nvarchar(200) NOT NULL,
        [descripcion] nvarchar(500) NULL,
        [activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [orden] int NOT NULL DEFAULT 0,
        [fecha_creacion] datetime2 NOT NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_AITema] PRIMARY KEY ([ai_tema_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [NombreCompleto] nvarchar(max) NOT NULL,
        [DocumentoIdentidad] nvarchar(max) NULL,
        [Telefono] nvarchar(max) NULL,
        [FechaRegistro] datetime2 NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [ConfiguracionWhatsApp] (
        [configuracion_whatsapp_id] int NOT NULL IDENTITY,
        [activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [tooltip_texto] nvarchar(200) NOT NULL,
        [mensaje_predeterminado] nvarchar(1000) NOT NULL,
        [mensaje_fuera_horario] nvarchar(500) NULL,
        [hora_inicio] time NULL,
        [hora_fin] time NULL,
        [mostrar_en_admin] bit NOT NULL DEFAULT CAST(1 AS bit),
        [mostrar_en_publico] bit NOT NULL DEFAULT CAST(1 AS bit),
        [paginas_mostrar] varchar(max) NULL,
        [fecha_actualizacion] datetime NOT NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_ConfiguracionWhatsApp] PRIMARY KEY ([configuracion_whatsapp_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [ContactoWhatsApp] (
        [contacto_whatsapp_id] int NOT NULL IDENTITY,
        [nombre] nvarchar(100) NOT NULL,
        [telefono] varchar(30) NOT NULL,
        [es_principal] bit NOT NULL DEFAULT CAST(0 AS bit),
        [activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [orden] int NOT NULL DEFAULT 0,
        [fecha_creacion] datetime NOT NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_ContactoWhatsApp] PRIMARY KEY ([contacto_whatsapp_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [ContenidoSitio] (
        [contenido_sitio_id] int NOT NULL IDENTITY,
        [clave] varchar(100) NOT NULL,
        [titulo] nvarchar(200) NULL,
        [subtitulo] nvarchar(300) NULL,
        [descripcion] nvarchar(max) NULL,
        [ruta_imagen] varchar(500) NULL,
        [metadatos_json] nvarchar(max) NULL,
        [fecha_actualizacion] datetime NOT NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_ContenidoSitio] PRIMARY KEY ([contenido_sitio_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [ExperienciaMultimediaItem] (
        [experiencia_multimedia_item_id] int NOT NULL IDENTITY,
        [carrusel_numero] int NOT NULL DEFAULT 1,
        [tipo] varchar(50) NOT NULL,
        [url] varchar(500) NOT NULL,
        [titulo] nvarchar(200) NULL,
        [orden] int NOT NULL DEFAULT 0,
        [activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [fecha_creacion] datetime NOT NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_ExperienciaMultimediaItem] PRIMARY KEY ([experiencia_multimedia_item_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [MetodosPago] (
        [metodo_pago_id] int NOT NULL IDENTITY,
        [nombre] varchar(50) NOT NULL,
        CONSTRAINT [PK_MetodosPago] PRIMARY KEY ([metodo_pago_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Patrocinadores] (
        [patrocinador_id] int NOT NULL IDENTITY,
        [empresa] varchar(100) NOT NULL,
        [contacto] varchar(100) NULL,
        [correo] varchar(100) NULL,
        [telefono] varchar(20) NULL,
        [descripcion] varchar(max) NULL,
        [url_pagina] varchar(500) NULL,
        [logo_url] varchar(500) NULL,
        [fecha_registro] datetime NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_Patrocinadores] PRIMARY KEY ([patrocinador_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Peleadores] (
        [peleador_id] int NOT NULL IDENTITY,
        [nombre] varchar(100) NOT NULL,
        [apodo] varchar(100) NULL,
        [nacionalidad] varchar(100) NULL,
        [peso] decimal(5,2) NULL,
        [fecha_nacimiento] date NULL,
        [peleas_ganadas] int NULL DEFAULT 0,
        [peleas_perdidas] int NULL DEFAULT 0,
        [descripcion] varchar(max) NULL,
        [foto_url] varchar(max) NULL,
        [estado] varchar(20) NULL DEFAULT 'activo',
        [fecha_registro] datetime NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_Peleadores] PRIMARY KEY ([peleador_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [SeccionesMultimedia] (
        [SeccionMultimediaId] int NOT NULL IDENTITY,
        [Titulo] nvarchar(200) NOT NULL,
        [Subtitulo] nvarchar(300) NULL,
        [Descripcion] nvarchar(max) NULL,
        [VideoPrincipalUrl] varchar(500) NULL,
        [VideoSecundarioUrl] varchar(500) NULL,
        [BeneficiosJson] nvarchar(max) NULL,
        [TextoBotonPrincipal] nvarchar(100) NULL,
        [UrlBotonPrincipal] varchar(500) NULL,
        [TextoBotonSecundario] nvarchar(100) NULL,
        [UrlBotonSecundario] varchar(500) NULL,
        [Orden] int NOT NULL DEFAULT 0,
        [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [FechaCreacion] datetime2 NULL DEFAULT ((getdate())),
        [FechaModificacion] datetime2 NULL,
        CONSTRAINT [PK_SeccionesMultimedia] PRIMARY KEY ([SeccionMultimediaId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [SeccionExperiencia] (
        [seccion_experiencia_id] int NOT NULL IDENTITY,
        [activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [titulo] nvarchar(200) NOT NULL,
        [subtitulo] nvarchar(300) NULL,
        [descripcion] nvarchar(1000) NULL,
        [beneficios_json] nvarchar(max) NULL,
        [texto_boton] nvarchar(100) NULL,
        [url_boton] varchar(500) NULL,
        [tiempo_autoplay] int NOT NULL DEFAULT 5,
        [fecha_creacion] datetime NOT NULL DEFAULT ((getdate())),
        [fecha_modificacion] datetime NULL,
        CONSTRAINT [PK_SeccionExperiencia] PRIMARY KEY ([seccion_experiencia_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [TiposEntrada] (
        [tipo_entrada_id] int NOT NULL IDENTITY,
        [nombre] varchar(50) NOT NULL,
        [precio] decimal(10,2) NOT NULL,
        [capacidad] int NOT NULL,
        CONSTRAINT [PK_TiposEntrada] PRIMARY KEY ([tipo_entrada_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Venue] (
        [venue_id] int NOT NULL IDENTITY,
        [nombre] nvarchar(200) NOT NULL,
        [svg_data] nvarchar(max) NULL,
        [imagen_previa] varchar(500) NULL,
        [imagen_plano] varchar(500) NULL,
        [descripcion] nvarchar(500) NULL,
        [orden] int NOT NULL DEFAULT 0,
        [activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_Venue] PRIMARY KEY ([venue_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Visitantes] (
        [visitante_id] int NOT NULL IDENTITY,
        [nombre] varchar(100) NOT NULL,
        [correo] varchar(150) NOT NULL,
        [telefono] varchar(30) NULL,
        [fecha_nacimiento] date NULL,
        [fecha_registro] datetime NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_Visitantes] PRIMARY KEY ([visitante_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Eventos] (
        [evento_id] int NOT NULL IDENTITY,
        [nombre] varchar(100) NOT NULL,
        [fecha] date NOT NULL,
        [hora] time NOT NULL,
        [lugar] varchar(200) NOT NULL,
        [descripcion] varchar(max) NULL,
        [imagen_url] varchar(255) NULL,
        [capacidad_maxima] int NOT NULL,
        [tickets_disponibles] int NOT NULL,
        [venue_id] int NULL,
        [estado_evento] varchar(20) NULL DEFAULT 'Próximo',
        [fecha_creacion] datetime NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_Eventos] PRIMARY KEY ([evento_id]),
        CONSTRAINT [FK_Evento_Venue] FOREIGN KEY ([venue_id]) REFERENCES [Venue] ([venue_id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [VenueZone] (
        [venue_zone_id] int NOT NULL IDENTITY,
        [venue_id] int NOT NULL,
        [nombre] nvarchar(100) NOT NULL,
        [precio] decimal(18,2) NOT NULL DEFAULT 0.0,
        [capacidad] int NOT NULL DEFAULT 0,
        [disponibles] int NOT NULL DEFAULT 0,
        [color] varchar(7) NOT NULL DEFAULT '#D4AF37',
        [orden] int NOT NULL DEFAULT 0,
        [svg_path_id] varchar(100) NULL,
        [activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [tiene_asientos_numerados] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_VenueZone] PRIMARY KEY ([venue_zone_id]),
        CONSTRAINT [FK_VenueZone_Venue] FOREIGN KEY ([venue_id]) REFERENCES [Venue] ([venue_id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AIChatHistorial] (
        [ai_chat_historial_id] bigint NOT NULL IDENTITY,
        [pregunta] nvarchar(max) NOT NULL,
        [respuesta] nvarchar(max) NOT NULL,
        [tiempo_respuesta_ms] int NOT NULL DEFAULT 0,
        [estado] varchar(50) NOT NULL DEFAULT 'Exitosa',
        [tema_detectado] nvarchar(200) NULL,
        [session_id] varchar(200) NULL,
        [visitante_id] int NULL,
        [fecha_consulta] datetime2 NOT NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_AIChatHistorial] PRIMARY KEY ([ai_chat_historial_id]),
        CONSTRAINT [FK_AIChatHistorial_Visitante] FOREIGN KEY ([visitante_id]) REFERENCES [Visitantes] ([visitante_id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Combates] (
        [combate_id] int NOT NULL IDENTITY,
        [evento_id] int NOT NULL,
        [peleador_rojo_id] int NOT NULL,
        [peleador_azul_id] int NOT NULL,
        [ganador_id] int NULL,
        [tipo_combate] varchar(100) NULL,
        [rounds] int NULL,
        [resultado] varchar(100) NULL,
        CONSTRAINT [PK_Combates] PRIMARY KEY ([combate_id]),
        CONSTRAINT [FK_Combates_Evento] FOREIGN KEY ([evento_id]) REFERENCES [Eventos] ([evento_id]),
        CONSTRAINT [FK_Combates_Ganador] FOREIGN KEY ([ganador_id]) REFERENCES [Peleadores] ([peleador_id]),
        CONSTRAINT [FK_Combates_PeleadorAzul] FOREIGN KEY ([peleador_azul_id]) REFERENCES [Peleadores] ([peleador_id]),
        CONSTRAINT [FK_Combates_PeleadorRojo] FOREIGN KEY ([peleador_rojo_id]) REFERENCES [Peleadores] ([peleador_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Compras] (
        [compra_id] int NOT NULL IDENTITY,
        [visitante_id] int NOT NULL,
        [evento_id] int NOT NULL,
        [metodo_pago_id] int NOT NULL,
        [fecha_compra] datetime NULL DEFAULT ((getdate())),
        [monto_total] decimal(10,2) NOT NULL,
        [estado_pago] varchar(20) NULL DEFAULT 'pendiente',
        CONSTRAINT [PK_Compras] PRIMARY KEY ([compra_id]),
        CONSTRAINT [FK_Compras_Evento] FOREIGN KEY ([evento_id]) REFERENCES [Eventos] ([evento_id]),
        CONSTRAINT [FK_Compras_MetodoPago] FOREIGN KEY ([metodo_pago_id]) REFERENCES [MetodosPago] ([metodo_pago_id]),
        CONSTRAINT [FK_Compras_Visitante] FOREIGN KEY ([visitante_id]) REFERENCES [Visitantes] ([visitante_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [EventoTiposEntrada] (
        [evento_tipo_id] int NOT NULL IDENTITY,
        [evento_id] int NOT NULL,
        [tipo_entrada_id] int NOT NULL,
        CONSTRAINT [PK_EventoTiposEntrada] PRIMARY KEY ([evento_tipo_id]),
        CONSTRAINT [FK_EventoTiposEntrada_Evento] FOREIGN KEY ([evento_id]) REFERENCES [Eventos] ([evento_id]),
        CONSTRAINT [FK_EventoTiposEntrada_TipoEntrada] FOREIGN KEY ([tipo_entrada_id]) REFERENCES [TiposEntrada] ([tipo_entrada_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Experiencias] (
        [ExperienciaId] int NOT NULL IDENTITY,
        [EventoId] int NOT NULL,
        [Nombre] nvarchar(200) NOT NULL,
        [Descripcion] nvarchar(max) NULL,
        [correo_visitante] varchar(200) NULL,
        [calificacion] int NULL,
        [Estado] nvarchar(20) NOT NULL DEFAULT N'Activa',
        [video_principal_url] varchar(500) NULL,
        [video_secundario_url] varchar(500) NULL,
        [fecha_creacion] datetime NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_Experiencias] PRIMARY KEY ([ExperienciaId]),
        CONSTRAINT [FK_Experiencias_Eventos_EventoId] FOREIGN KEY ([EventoId]) REFERENCES [Eventos] ([evento_id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Patrocinios] (
        [patrocinio_id] int NOT NULL IDENTITY,
        [patrocinador_id] int NOT NULL,
        [url_pagina] varchar(255) NULL,
        [logo] varchar(255) NULL,
        [flyer] varchar(255) NULL,
        [fecha_inicio] date NOT NULL,
        [fecha_fin] date NOT NULL,
        [costo] decimal(10,2) NOT NULL,
        [evento_id] int NOT NULL,
        [mensaje_promocional] varchar(500) NULL,
        [productos_json] varchar(max) NULL,
        [activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [orden] int NOT NULL DEFAULT 0,
        CONSTRAINT [PK_Patrocinios] PRIMARY KEY ([patrocinio_id]),
        CONSTRAINT [FK_Patrocinios_Evento] FOREIGN KEY ([evento_id]) REFERENCES [Eventos] ([evento_id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Patrocinios_Patrocinador] FOREIGN KEY ([patrocinador_id]) REFERENCES [Patrocinadores] ([patrocinador_id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [HistorialCombates] (
        [historial_id] int NOT NULL IDENTITY,
        [combate_id] int NOT NULL,
        [metodo_victoria] varchar(100) NULL,
        [rounds_totales] int NULL,
        [observaciones] varchar(max) NULL,
        CONSTRAINT [PK_HistorialCombates] PRIMARY KEY ([historial_id]),
        CONSTRAINT [FK_HistorialCombates_Combate] FOREIGN KEY ([combate_id]) REFERENCES [Combates] ([combate_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [DetalleCompra] (
        [detalle_id] int NOT NULL IDENTITY,
        [compra_id] int NOT NULL,
        [tipo_entrada_id] int NOT NULL,
        [cantidad] int NOT NULL,
        [precio_unitario] decimal(10,2) NOT NULL,
        [subtotal] decimal(10,2) NOT NULL,
        CONSTRAINT [PK_DetalleCompra] PRIMARY KEY ([detalle_id]),
        CONSTRAINT [FK_DetalleCompra_Compra] FOREIGN KEY ([compra_id]) REFERENCES [Compras] ([compra_id]),
        CONSTRAINT [FK_DetalleCompra_TipoEntrada] FOREIGN KEY ([tipo_entrada_id]) REFERENCES [TiposEntrada] ([tipo_entrada_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Tickets] (
        [ticket_id] int NOT NULL IDENTITY,
        [compra_id] int NOT NULL,
        [evento_id] int NOT NULL,
        [tipo_entrada_id] int NOT NULL,
        [codigo_unico] varchar(30) NOT NULL,
        [codigo_qr] varchar(500) NOT NULL,
        [token_hash] varchar(200) NOT NULL,
        [tipo_ticket] varchar(50) NULL,
        [zona] varchar(50) NULL,
        [precio] decimal(10,2) NULL,
        [estado] varchar(20) NULL DEFAULT 'Activo',
        [fecha_generacion] datetime NULL DEFAULT ((getdate())),
        [pdf_path] varchar(500) NULL,
        [qr_path] varchar(500) NULL,
        CONSTRAINT [PK_Tickets] PRIMARY KEY ([ticket_id]),
        CONSTRAINT [FK_Tickets_Compra] FOREIGN KEY ([compra_id]) REFERENCES [Compras] ([compra_id]),
        CONSTRAINT [FK_Tickets_Evento] FOREIGN KEY ([evento_id]) REFERENCES [Eventos] ([evento_id]),
        CONSTRAINT [FK_Tickets_TipoEntrada] FOREIGN KEY ([tipo_entrada_id]) REFERENCES [TiposEntrada] ([tipo_entrada_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Asistente] (
        [asistente_id] int NOT NULL IDENTITY,
        [ticket_id] int NOT NULL,
        [nombre] nvarchar(100) NOT NULL,
        [apellido] nvarchar(100) NOT NULL,
        [tipo_documento] varchar(20) NOT NULL,
        [numero_documento] varchar(30) NOT NULL,
        [correo] varchar(150) NOT NULL,
        [celular] varchar(30) NULL,
        [fecha_nacimiento] date NULL,
        CONSTRAINT [PK_Asistente] PRIMARY KEY ([asistente_id]),
        CONSTRAINT [FK_Ticket_Asistente] FOREIGN KEY ([ticket_id]) REFERENCES [Tickets] ([ticket_id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Descuentos] (
        [descuento_id] int NOT NULL IDENTITY,
        [ticket_id] int NOT NULL,
        [cantidad_minima] int NOT NULL,
        [porcentaje_descuento] decimal(5,2) NOT NULL,
        CONSTRAINT [PK_Descuentos] PRIMARY KEY ([descuento_id]),
        CONSTRAINT [FK_Descuentos_Ticket] FOREIGN KEY ([ticket_id]) REFERENCES [Tickets] ([ticket_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AIChatHistorial_visitante_id] ON [AIChatHistorial] ([visitante_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_AIContextoConfig_Clave] ON [AIContextoConfig] ([clave]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Asistente_Ticket] ON [Asistente] ([ticket_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Combates_evento_id] ON [Combates] ([evento_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Combates_ganador_id] ON [Combates] ([ganador_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Combates_peleador_azul_id] ON [Combates] ([peleador_azul_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Combates_peleador_rojo_id] ON [Combates] ([peleador_rojo_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Compras_evento_id] ON [Compras] ([evento_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Compras_metodo_pago_id] ON [Compras] ([metodo_pago_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Compras_visitante_id] ON [Compras] ([visitante_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_ContenidoSitio_Clave] ON [ContenidoSitio] ([clave]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Descuentos_ticket_id] ON [Descuentos] ([ticket_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DetalleCompra_compra_id] ON [DetalleCompra] ([compra_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DetalleCompra_tipo_entrada_id] ON [DetalleCompra] ([tipo_entrada_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Eventos_venue_id] ON [Eventos] ([venue_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EventoTiposEntrada_tipo_entrada_id] ON [EventoTiposEntrada] ([tipo_entrada_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_EventoTiposEntrada] ON [EventoTiposEntrada] ([evento_id], [tipo_entrada_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Experiencias_EventoId] ON [Experiencias] ([EventoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_HistorialCombates_combate_id] ON [HistorialCombates] ([combate_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Patrocinios_evento_id] ON [Patrocinios] ([evento_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Patrocinios_patrocinador_id] ON [Patrocinios] ([patrocinador_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tickets_codigo_unico] ON [Tickets] ([codigo_unico]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_compra_id] ON [Tickets] ([compra_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_evento_id] ON [Tickets] ([evento_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_tipo_entrada_id] ON [Tickets] ([tipo_entrada_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Tickets_CodigoQR] ON [Tickets] ([codigo_qr]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_TiposEntrada_Nombre] ON [TiposEntrada] ([nombre]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_VenueZone_venue_id] ON [VenueZone] ([venue_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Visitantes_Correo] ON [Visitantes] ([correo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261106182136_InitialCreate', N'10.0.14');
END;

COMMIT;
GO
```
