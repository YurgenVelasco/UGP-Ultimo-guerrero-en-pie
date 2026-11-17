# Entrega 6.2 — Acceso administrativo privado

Fecha: 2026-11-13 (Colombia). Alcance: acceso y panel mínimo sin CRUD.

## Resultado y límites

Se implementó el acceso mediante **Identity existente**, la política `AdministracionUGP`, un dashboard Blazor SSR y una API de estado de solo lectura. No hay registro público, contraseña inicial, creación de roles ni bypass de autenticación.

La lectura agregada de la conexión resuelta para Development encontró **0 usuarios Identity, 0 usuarios con MFA y 0 asignaciones de roles**; la consulta de roles no devolvió filas. No se imprimieron nombres de cuentas, contraseñas, hashes ni conexión. El intento restringido inicial falló con SqlException; la lectura autorizada fuera del sandbox completó correctamente. Esto describe únicamente esa conexión, no otras bases o producción.

**No se puede iniciar sesión real todavía:** falta un procedimiento aprobado de provisión de la primera cuenta/rol y configurar `Administracion:RolPermitido`. Ningún rol se concede por defecto. La ausencia de configuración o un fallo de consulta deniega acceso. No se modificaron cuentas existentes ni el esquema.

## Arquitectura reutilizada

- `ApplicationUser`, UserManager, SignInManager, cookies Identity y ApplicationDbContext existentes.
- La página de acceso es un componente Razor estático renderizado desde endpoints HTTP. El manejo de contraseña/cookies se realiza en el servidor HTTP, no en un circuito interactivo.
- Dashboard `/administracion`: `[Authorize(Policy = "AdministracionUGP")]`, excluido de navegación interactiva y sin `InteractiveServer`. Evita mantener sesiones privilegiadas largas dentro de circuitos.
- Routes usa AuthorizeRouteView para comprobar autorización de componentes. El middleware verifica también acceso HTTP.
- `AdminOperations` aplica la misma política dentro del servicio, además del endpoint. Toda futura operación privilegiada debe pasar por esa guardia; no basta ocultar botones.
- Compras, devoluciones y configuración administrativa NO tienen nuevas operaciones en esta entrega. No se conectaron directamente métodos internos de tickets ni se concedieron permisos administrativos mediante CompraAccess. Se conservan las protecciones existentes y el checkout deshabilitado.

Referencias consultadas: [seguridad Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-10.0) y [antifalsificación ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery). Se usa validación explícita IAntiforgery en los POST HTTP.

## Rutas y configuración

| Ruta | Método | Protección |
|---|---|---|
| `/acceso-ugp` por defecto | GET | Formulario privado sin enlaces públicos, noindex/nofollow, sin caché, limitador |
| Misma ruta configurable | POST | HTTPS, token CSRF, contraseña Identity, bloqueo de cuenta, rol confirmado en servidor y MFA cuando corresponde |
| `/administracion` | GET | Política AdministracionUGP; dashboard informativo sin CRUD |
| `/api/administracion/estado` | GET | Política y guardia interna; solo texto de estado, sin compradores |
| `/administracion/salir` | POST | Autenticación, HTTPS y CSRF; SignOutAsync y redirección local fija |

Configuración privada opcional mediante User Secrets o variables de entorno (no se escribió durante esta entrega):

- `Administracion:RutaAcceso` / `Administracion__RutaAcceso`: admite un único segmento `/acceso-` seguido de 3–60 caracteres minúsculos, dígitos o guiones. Rechaza configuraciones inválidas al arrancar para impedir colisiones/rutas externas. Valor por defecto `/acceso-ugp`.
- `Administracion:RolPermitido` / `Administracion__RolPermitido`: nombre exacto de un rol Identity previamente aprobado y existente. **Sin valor predeterminado**. No introducir un nombre esperando que eso cree/asigne el rol.
- `Administracion:ExigirMfa`: permite exigir MFA también en Development. Fuera de Development se exige siempre, aunque la configuración indique false.

No se añadió configuración a appsettings ni enlaces al menú, Home o footer. Cambiar la ruta reduce exposición casual, pero no es un control de autorización. Los accesos anónimos a panel/API devuelven 401 sin redirigir ni revelar la ruta privada; una identidad autenticada sin permiso recibe 403.

## Controles de seguridad

1. Cookie de aplicación Identity `__Host-UGP.Identity`, Secure Always, HttpOnly, SameSite Strict, Path `/`, no persistente. Sin renovación deslizante; 20 minutos absolutos. Las cookies de autorización por compra no se cambiaron.
2. Contraseña validada por SignInManager con lockoutOnFailure; máximo 5 fallos y bloqueo 15 minutos. La cuenta debe tener LockoutEnabled; no se habilita ese atributo automáticamente.
3. Límite global de 20 solicitudes por minuto en acceso GET/POST, sin cola, respuesta 429. Protege también frente a cambios de IP; puede causar indisponibilidad temporal por abuso. Es memoria local por proceso, no un control distribuido.
4. POST de acceso/salida exige CSRF y HTTPS; tamaño máximo del cuerpo de login 8192 bytes cuando el servidor expone el límite, además de límites de campos. No se cambian límites globales de formularios públicos.
5. Autorización comprueba esquema Identity, vencimiento firmado de sesión, security stamp, bloqueo actual y pertenencia al rol consultada en servidor. Revocaciones de rol/stamp no esperan a una nueva sesión. Indisponibilidad de SQL implica denegación.
6. MFA: admite el código del autenticador Identity de una cuenta ya inscrita. Si TwoFactorEnabled está activo, se exige código incluso en Development; si el entorno no es Development, también se rechaza una cuenta sin MFA. No hay inscripción, recuperación ni desactivación automática de MFA en esta entrega.
7. Sin returnUrl arbitrario: después del login solo se redirige al panel local. Error genérico sin revelar si falló usuario, contraseña, rol o MFA. No se registran credenciales/códigos.
8. Cabeceras no-store, no-referrer y X-Frame-Options DENY para administración; login además CSP restrictiva sin scripts ni contenido externo.
9. Auditoría estructurada con ILogger: LoginSucceeded (ID interno/MFA), LoginDenied, LoginUnavailable, Logout, StatusRead, OperationDenied y RequestDenied. No incluye contraseñas, tokens, códigos MFA ni nombres enviados en fallos. No se creó una tabla de auditoría; retención durable y control de acceso al destino de logs quedan pendientes antes de producción.

No hay una «clave maestra» ni una ruta de prueba en la aplicación que emita cookies. Los usuarios de fixture y la ruta de modificación ficticia existen exclusivamente en el ejecutable Tests, excluido de la compilación/publicación web por el csproj existente.

## Archivos de la entrega

Modificados:

- `Program.cs`: registro de política/guardia, cookies, lockout, proveedores MFA, limitador, middleware y endpoints. Conserva cambios previos de reservas/seguridad.
- `Components/Routes.razor`: AuthorizeRouteView.
- `Tests/Reservas/Program.cs`: opciones aisladas `--admin-http` y `--admin-inventory`.

Creados:

- `Services/Administration/AdminAccess.cs`: configuración, requisito, handler y guardia de servicio.
- `Services/Administration/AdminEndpoints.cs`: login/logout y API de estado.
- `Components/Administration/AdminLogin.razor`: formulario SSR sin registro público.
- `Components/Pages/Admin/Dashboard.razor`: panel mínimo protegido.
- `Tests/Reservas/AdminHttpChecks.cs`: Identity real con almacén de usuarios exclusivamente en memoria y HTTPS loopback.
- `Tests/Reservas/AdminInventory.cs`: lectura agregada explícita de Identity; no ejecutada en arranque.
- Este informe.

## Validación ejecutada

| Escenario | Resultado | Evidencia y alcance |
|---|---|---|
| Build aplicación y suite | PASA | build Tests/Reservas/Reservas.Checks.csproj con referencia al proyecto, --no-restore, UseSharedCompilation=false, salida artifacts/admin62-final: 0 errores, 0 advertencias |
| Anónimo dashboard/API | PASA HTTP | Host de prueba devuelve 401; también UGP real HTTPS local devolvió 401 en ambas rutas |
| Administrador autenticado | PASA aislada HTTP/Identity | Contraseña aleatoria, hashing Identity, cookie real, dashboard fixture y API protegida 200; no prueba de cuenta SQL |
| Usuario sin privilegios | PASA aislada | Login no concede cookie administrativa; ticket autenticado de fixture sin rol recibe 403 en API |
| Modificación sin permiso | PASA aislada | Ruta de mutación ficticia solo en Tests: anónimo 401, sin rol 403, contador de escrituras 0 |
| CSRF | PASA aislada | Login y logout sin token devuelven 400; formulario SSR genera token y solicitudes válidas completan |
| Cookie segura | PASA aislada | Set-Cookie comprobado: Secure, HttpOnly, SameSite Strict |
| Expiración | PASA aislada | Ticket Identity vencido generado por fixture rechazado con 401; no se esperaron 20 minutos de reloj real |
| Revocación de rol | PASA aislada | Cambio en almacén de fixture hace que la siguiente consulta reciba 403 |
| Logout | PASA aislada | POST con CSRF elimina sesión y dashboard posterior devuelve 401 |
| Intentos repetidos | PASA aislada | 5 fallos bloquean cuenta en memoria; limitador devuelve 429 |
| MFA de producción | PASA parcial aislada | Política exige MFA fuera de Development y rechaza principal sin segundo factor; login con autenticador real pendiente |
| Rol ausente | PASA aislada | No existe rol implícito por configuración predeterminada |
| UGP real `/acceso-ugp` | PASA HTTP | HTTPS localhost:7196, respuesta 200 y token CSRF presente, sin enviar credenciales |
| Página pública real `/contacto` | PASA HTTP | Respuesta 200 sin sesión, sin enlace privado |
| Suite administrativa | PASA | `--admin-http`: 28 comprobaciones, incluidas comprobaciones repetidas de token en cada intento; no son 28 escenarios independientes |
| Suite previa | PASA | `--isolated`: 30 comprobaciones; `--gallery`: 24; sin ejecución SQL de pruebas |
| git diff --check | PASA | Sin errores de whitespace |

Las pruebas de HTTP usaron Kestrel HTTPS real con certificado de prueba validado por huella exacta en el cliente, sin desactivar validación global. La primera ejecución restringida encontró SocketException; la siguiente detectó incompatibilidad del certificado efímero con Windows. Se corrigió únicamente el helper de tests usando carga PKCS12 UserKeySet, tras lo cual la suite pasó. No se cambió TLS de la aplicación.

El host de UGP se inició con overrides de proceso para mantener Reservas y Webhooks en false, sin persistir flags. Se detuvo al finalizar. La última edición de auditoría de denegaciones se compiló y pasó la suite final; las consultas HTTP al host completo se realizaron antes de esa adición de logging. No se afirma revisión visual de navegador ni login completo contra SQL. No se realizaron pagos, scripts SQL ni migraciones.

## Pendientes antes de uso operativo

1. Aprobar la provisión controlada de la primera cuenta y rol Identity, su asignación y LockoutEnabled; sin contraseña predeterminada ni registro público. Configurar RolPermitido solo después. No se efectuó esta provisión.
2. Inscribir MFA por un procedimiento seguro y probar código válido/inválido, recuperación y revocación con una cuenta autorizada. La política de producción ya falla de forma cerrada sin MFA.
3. Repetir acceso completo con SQL aislado y cuenta autorizada, render del dashboard real, renovación de claves Data Protection, logout/expiración y revocación. Las pruebas con almacén en memoria no prueban persistencia/concurrencia Identity SQL.
4. Confirmar destino durable y retención de auditoría. El cierre de sesión elimina la cookie del navegador, pero no invalida una copia robada del ticket por sí mismo: esa copia vence a los 20 minutos o se rechaza tras revocación de rol/stamp/bloqueo. Evaluar revocación por sesión si es requisito antes de producción.
5. Ajustar protección antiabuso en despliegue de múltiples instancias y validar HTTPS/proxy confiable. No hay cambios globales de red ni nuevas dependencias.
6. No exponer métodos internos de compra/ticket, devoluciones o configuración mediante futuros CRUD sin guardia servidor, CSRF y pruebas de autorización por operación. No hay CRUD en esta entrega.

## Git y dictamen

Rama main, HEAD `62bdf1d864aa02de4e5e52442083d74dbb43c494`, ahead 1 respecto a referencia local origin/main. Índice sin nuevos cambios preparados. Los archivos previos modificados/sin seguimiento se conservaron; Program y Tests acumulan entregas anteriores. Sin commits, push, reset, clean ni stash. No se cambiaron credenciales, esquema, Wompi ni flags persistidos.

**Dictamen: acceso implementado y probado en HTTPS aislado; cerrado para uso real hasta provisión autorizada de cuenta/rol y validación MFA/SQL. No listo para producción.** Detenerse antes del CRUD.
