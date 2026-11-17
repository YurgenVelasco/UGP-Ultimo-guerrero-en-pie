# Entrega 6.2.1 — Provisionamiento y validación real

**Cierre de validación: 2026-11-14, Colombia. Dictamen: APROBADA CON OBSERVACIONES exclusivamente para los escenarios administrativos probados en UGP_Integracion_Test. No es autorización de producción, CRUD ni pagos.**

## Dictamen final y evidencia de expiración

Se inspeccionó `artifacts/admin621-evidence/expiry-20261114050815.txt`: **9 comprobaciones PASA, ninguna FALLA**. Una sesión nueva con contraseña y TOTP recibió 200 en dashboard/API; después de 21 minutos reales ambos devolvieron 401. El reenvío deliberado del mismo ticket vencido, conservado exclusivamente en memoria, también obtuvo 401 en ambos endpoints. No se modificaron TTL, reloj, MFA ni stamp durante esta prueba.

Junto con las 17 comprobaciones del archivo de login posterior a MFA, hay **26 comprobaciones HTTPS reales aprobadas** (incluyen precondiciones repetidas, no 26 escenarios independientes). Se acredita login con MFA, denegación sin segundo factor, denegación 403 sin rol en SQL, logout con CSRF, revocación y expiración efectiva del lado servidor.

Lectura SQL final: 1 usuario, 1 rol, 1 asignación y 1 usuario MFA. Rol restaurado y MFA conservado. La base principal no se modificó. Host temporal detenido al cerrar; sus overrides de rol/MFA no se persistieron. Checkout y Wompi no fueron habilitados. No se conservaron contraseñas, semillas, códigos ni tickets en la evidencia.

Observaciones pendientes: implementar y probar el mecanismo de recuperación propuesto; auditoría durable y revisión de despliegue antes de producción. La denegación de código mal formado no equivale a probar todos los casos de TOTP numérico incorrecto/reutilizado. Las pruebas de bloqueo por intentos siguen acreditadas por suite aislada, no por un bloqueo intencional de esta cuenta SQL. No se afirma cobertura total de seguridad.

Build de aplicación y pruebas: 0 errores/advertencias en la compilación utilizada. git diff --check correcto al cierre. Rama main, HEAD 62bdf1d, ahead 1 respecto a origin/main local; árbol previo conservado y sin cambios preparados nuevos. Sin commits ni push. En este cierre se actualizó únicamente este informe; las herramientas y cambios administrativos corresponden al trabajo previo de esta misma entrega.

## Resultado real posterior a MFA — evidencia inspeccionada

Se leyó `artifacts/admin621-evidence/login-20261114044522.txt` (nombre UTC, fecha local Colombia 2026-11-13): **17 comprobaciones PASA**, sin líneas FALLA. Se verificaron contraseña + TOTP auténtico, dashboard Blazor 200, API 200, denegación sin TOTP/con código mal formado, misma sesión autenticada sin rol SQL 403 en dashboard/API, restauración del rol, atributos de cookie, logout sin CSRF 400, logout válido 302 y acceso posterior 401. Un ticket legítimo retenido sólo en memoria permanecía vigente antes de revocar SecurityStamp y fue rechazado después por ambos endpoints.

Lectura SQL posterior independiente: **1 usuario, 1 rol, 1 asignación y 1 usuario MFA**. No se duplicaron cuentas ni quedó retirado el rol. No se consultaron semillas ni se registraron credenciales. Checkout sigue deshabilitado; el host aislado conserva MFA obligatorio y webhook false.

**Expiración completada:** el modo `--admin-integration-expiry` en `artifacts/admin621-expiry` comprobó los 401 descritos en el dictamen final. No requiere otra entrada de contraseña ni TOTP.

Compilación de la herramienta: 0 errores/advertencias. Las secciones posteriores que indican pruebas MFA pendientes describen etapas anteriores; este apartado actualiza esos resultados. Recuperación sigue siendo propuesta, no funcionalidad implementada. No se ejecutaron migraciones, commits ni push.

## Bitácora histórica — estados intermedios, sustituidos por el dictamen final

Los apartados siguientes conservan decisiones, propuesta de recuperación y pasos anteriores. Sus expresiones «pendiente» relativas a login, MFA inscrito, rol, revocación y expiración corresponden al momento en que se registraron; el resultado vigente es el dictamen final de este documento.

### Verificación posterior a inscripción MFA

Lectura independiente posterior a la confirmación del responsable: **1 usuario, 1 rol, 1 asignación y 1 usuario con TwoFactorEnabled** en UGP_Integracion_Test. No se consultaron semillas ni códigos. El host HTTPS mantiene disponible el login (200). El último arranque controlado exige MFA y mantiene reservas/webhook false.

Preparada la compilación `artifacts/admin621-postmfa` (0 errores/advertencias) y comprobado git diff --check. El modo `--admin-integration-login-mfa` ahora solicita el TOTP inmediatamente antes del login válido, después de las pruebas sin código/código mal formado, para reducir caducidad durante la prueba. Introducción exclusivamente oculta en consola.

Además de dashboard/API, retiro/restauración temporal de rol y logout, se añadió prueba real de revocación: conserva un ticket legítimo sólo en memoria, verifica su vigencia, actualiza SecurityStamp mediante UserManager en la base aislada y comprueba denegación del ticket previo. No crea ni imprime cookies artificiales. Se conserva el rol; MFA no se desactiva. **La ejecución de estas pruebas posteriores a MFA está pendiente de la contraseña/TOTP que el responsable debe introducir localmente.** No se atribuyen resultados a código meramente preparado.

Expiración: la suite aislada anterior verificó ticket expirado; falta medir los 20 minutos reales sin renovación en una sesión autenticada con TOTP. La revocación por stamp no equivale a esa prueba temporal. No se acorta la duración ni se deshabilita MFA para aprobarla.

### Propuesta de recuperación MFA (no implementada ni ejecutada)

Reutilizar códigos de recuperación de Identity de un solo uso, generados únicamente después de reautenticación con contraseña y TOTP válidos, con visualización privada una vez y custodia offline del responsable. No incluirlos en logs, backups de evidencia ni documentación. El almacenamiento de tokens Identity existente requiere protección de acceso y respaldo; no asumir que sus valores están hasheados por defecto.

Para recuperar: contraseña válida más código de recuperación válido, limitación de intentos y auditoría sin secretos; consumir el código mediante APIs Identity con garantías frente a reutilización/concurrencia verificadas. Conceder sólo una sesión restringida de recuperación, sin acceso al panel ni operaciones comerciales. Permitir inscribir/verificar un nuevo autenticador; mantener la cuenta protegida por la política MFA durante el proceso. Tras verificarlo, revocar sesiones/stamp, invalidar códigos anteriores y emitir un juego nuevo de forma privada. Si la transición segura exige persistencia adicional, diseñarla y solicitar autorización antes de cambiar SQL. No usar SetTwoFactorEnabled(false) como atajo.

Si se pierden tanto autenticador como códigos, mantener el bloqueo y exigir procedimiento de verificación de identidad con doble aprobación y trazabilidad, definido por el propietario; no contraseñas maestras, enlaces públicos de emergencia, aprobación por simple correo ni cuenta compartida. La recuperación requiere una entrega autorizada y pruebas negativas, de reutilización, concurrencia y revocación; el login actual todavía no admite códigos de recuperación.

Referencia: [Identity y códigos de recuperación](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0). Esta referencia no implica habilitar los endpoints públicos de registro del ejemplo.

## Actualización — cierre de seguridad y preparación MFA

El responsable informó que las nueve comprobaciones HTTPS de login real pasaron. El host anterior registró operaciones autenticadas y logout. No se encontró el directorio artifacts/admin621-evidence en la raíz durante esta revisión; esos nueve resultados se atribuyen al reporte del responsable, no a un archivo de evidencia inspeccionado.

**Discrepancia de configuración resuelta:** el diagnóstico carga la configuración de su propio proceso (appsettings, User Secrets y variables heredadas). El host aislado recibió `Administracion__RolPermitido=Yurgenvelasco` sólo en su proceso; no se escribió en User Secrets ni appsettings. Por eso el diagnóstico decía False aunque el host autorizaba por rol. Se corrigió el mensaje para explicitar que no inspecciona el host y se agregó un log de arranque de booleanos no sensibles.

Se detuvo el host previo y se inició la compilación actual en HTTPS localhost:7197 contra la base aislada. Evidencia del arranque: **entorno=Development; rol configurado=True; MFA obligatorio=True; reservas habilitadas=False; webhook habilitado=False**. `Administracion:ExigirMfa=true` es un override exclusivo del proceso. No se habilitó un entorno principal ni se deshabilitó MFA para las pruebas.

La política no se limita a RequireAuthenticatedUser: AdminAuthorizationHandler comprueba security stamp, vigencia, bloqueo, MFA requerido y `UserManager.IsInRoleAsync(user, settings.Role)` contra el almacén. Sin rol o sin configuración no llama a Succeed. Dashboard exige la política, API exige la política y AdminOperations repite la autorización en el servidor.

Resultados nuevos:

- Lectura SQL: sigue existiendo 1 usuario, 1 rol, 1 asignación, 0 usuarios MFA.
- Suite HTTPS aislada ampliada: **29 comprobaciones PASA**, incluida respuesta **403 tanto en dashboard como API para identidad autenticada sin rol**. Almacén en memoria; no presentar esta prueba como SQL real.
- Compilación actual: 0 errores, 0 advertencias.
- Preparada prueba SQL/HTTPS `--admin-integration-login-mfa`: tras login real, retira temporalmente el rol de la misma cuenta marcada de integración, comprueba 403 conservando la sesión y restaura el rol en finally. No crea una segunda cuenta ni cambia la contraseña. **Todavía no ejecutada**; una interrupción abrupta podría dejar el rol retirado, en cuyo caso detenerse y verificar antes de restaurar mediante Identity.
- Ese modo prueba además código ausente y código mal formado antes del login con TOTP del responsable. Implica dos intentos fallidos; no ejecutarlo repetidamente sobre una cuenta bloqueada. No prueba aún un código numérico de otra ventana temporal ni recuperación.
- Activación MFA y cambio de security stamp de la utilidad local se agruparon en una transacción. No se altera un MFA ya inscrito.

**Acción privada pendiente:** el responsable debe ejecutar `dotnet .\artifacts\admin621-mfa\Reservas.Checks.dll --admin-integration-mfa`, introducir su contraseña localmente y añadir en Google Authenticator la clave mostrada sólo en esa consola (tipo basado en tiempo). Introducir allí el TOTP. No enviar clave, captura ni código al chat. No se inspeccionará la pantalla que contiene la semilla.

Tras la inscripción, verificar por conteo SQL que MFA está activo y ejecutar `--admin-integration-login-mfa` con un código nuevo. Hasta entonces no se declaran aprobados MFA real, sesión posterior, denegación sin rol SQL ni cierre posterior a MFA. El host permanece temporalmente activo y exige MFA mientras se espera esta intervención. Recuperación sigue pendiente de mecanismo aprobado; no hay bypass.

Cambios de esta revisión: Program.cs (log seguro de estado efectivo), AdminIntegrationPreflight.cs (aclaración de alcance), AdminIntegrationMfa.cs (transacción), AdminIntegrationLogin.cs y Program.cs de Tests (modo de validación MFA/rol), AdminHttpChecks.cs (dashboard sin rol), este informe. Árbol previo preservado; sin commits, push ni migraciones.

## Entorno y autorización

- Destino único: `(localdb)\MSSQLLocalDB`, `UGP_Integracion_Test`, autenticación Windows.
- Usuario y rol autorizados por el responsable: `Yurgenvelasco` / `Yurgenvelasco`.
- Aplicación TOTP elegida: Google Authenticator.
- Host de pruebas: `https://localhost:7197`, Development, conexión a la base aislada, rol permitido configurado únicamente en el proceso del host. Reservas y webhook deshabilitados.
- La configuración persistida sigue sin RolPermitido. El preflight muestra ese estado; no contradice el override del host aislado.

## Procedimiento

Las herramientas manuales de Tests/Reservas no se ejecutan al arrancar UGP. Rechazan entrada/salida redirigida para evitar captura de credenciales. La contraseña se introduce con Console.ReadKey sin eco; no se guarda en archivos ni documentos.

`--admin-integration-preflight` verifica destino, columnas/tipos/nulabilidad EF de Identity, presencia de claves primarias, índices únicos de nombres y relaciones habilitadas, y obtiene conteos sin datos privados. No ejecuta DDL.

`--admin-integration-provision` utiliza UserManager y RoleManager, validadores y hash de Identity. Crea usuario, rol, asignación y marca de prueba dentro de una transacción. Una repetición reconoce la marca y no cambia contraseña, MFA ni asignaciones; rechaza cuentas ajenas y la creación de una segunda cuenta. La idempotencia está implementada, pero aún no se ha ejecutado la repetición contra esta cuenta.

El primer error genérico se corrigió en la herramienta para mostrar mensajes constantes de validación sin entradas ni excepciones internas. Se observó rechazo por longitud menor de 12 y falta de símbolo. Una lectura posterior confirmó cero usuarios/roles/asignaciones. No se relajó la política. El responsable repitió la entrada con una contraseña válida.

## Evidencia actual

| Comprobación | Resultado | Evidencia |
|---|---|---|
| LocalDB y destino aislado | PASA, lectura real | DB_NAME coincide y SERVERPROPERTY IsLocalDB=1 |
| Esquema Identity | PASA comprobaciones de metadatos | Columnas/tipos/nulabilidad; claves e índices revisados; sin cambios de esquema |
| Provisionamiento | PASA, SQL real | Responsable observó confirmación Identity; lectura independiente posterior: 1 usuario, 1 rol, 1 asignación |
| MFA inscrito | PENDIENTE | Conteo actual de usuarios MFA: 0 |
| Checkout bloqueado | PASA configuración | Reservas:Habilitadas=False; overrides del host mantienen webhook deshabilitado |
| Login SSR disponible | PASA HTTPS real | GET /acceso-ugp devuelve 200 |
| Dashboard/API anónimos | PASA HTTPS real | Ambos devolvieron 401 antes del provisionamiento |
| Página pública | PASA HTTPS real | /contacto devuelve 200 sin sesión |
| Login real con contraseña del responsable | PENDIENTE | Requiere entrada local en --admin-integration-login |
| Dashboard real autenticado, API y logout | PENDIENTE | No sustituir por las pruebas en memoria |
| Sin rol, expiración, revocación y bloqueo SQL | PENDIENTE integración | La suite aislada cubre esos casos, pero no esta cuenta SQL |
| TOTP correcto/incorrecto, sin MFA y recuperación | PENDIENTE | No se ha inscrito Google Authenticator ni usado códigos reales |
| Build | PASA | Compilación aplicación y ejecutable Tests: 0 errores/advertencias |
| Suite administrativa previa | PASA aislada | 28 comprobaciones HTTPS con Identity y almacén en memoria; no son pruebas de esta cuenta |
| git diff --check | PASA en revisión previa al provisionamiento | Repetir al cierre |

No se ha leído ni impreso PasswordHash. El uso del hash de Identity está acreditado por UserManager.CreateAsync; falta verificar mediante su API la contraseña de la cuenta real durante la prueba local.

## Herramientas y comandos locales

Ejecutar desde la raíz del repositorio, sin transcripción de terminal ni redirección:

```powershell
dotnet .\artifacts\admin621-diagnostic\Reservas.Checks.dll --admin-integration-login
dotnet .\artifacts\admin621-diagnostic\Reservas.Checks.dll --admin-integration-mfa
```

Primero comprobar login sin código antes de inscribir MFA. La inscripción muestra la clave únicamente en la consola local del responsable para introducirla en Google Authenticator; no capturar pantalla, copiar al chat ni registrar la clave. Solo activa MFA después de verificar el código del autenticador y actualiza el security stamp. No rota una clave ya inscrita. Repetir después login con un TOTP nuevo.

La herramienta HTTPS guarda exclusivamente resultados fijos PASA/FALLA en artifacts/admin621-evidence; nunca guarda credenciales, códigos, cookies ni cuerpos HTTP. La recuperación todavía no tiene mecanismo integrado aprobado; no se crean códigos que el login no acepta.

## Archivos y Git

Agregados a Tests/Reservas: AdminIntegrationPreflight.cs, AdminIntegrationProvision.cs, AdminIntegrationMfa.cs y AdminIntegrationLogin.cs. Program.cs del ejecutable incorpora los modos manuales. Este informe es nuevo. No se modificó código funcional de UGP en esta etapa.

Se conserva el árbol previo, rama main y HEAD 62bdf1d. Sin commits ni push, migraciones ni modificaciones a la base principal. Ninguna contraseña predeterminada o de producción. No hay CRUD nuevo.

## Dictamen provisional

Cuenta aislada provisionada y confirmada por SQL. **Validación real de autenticación/MFA todavía en curso; no habilitar producción ni declarar cerrada la Entrega 6.2.1.** Al cerrar deben detenerse los hosts temporales y actualizar esta evidencia con los resultados efectivos.
