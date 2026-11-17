# UGP — Último Guerrero en Pie

Aplicación web para publicar eventos de combate, consultar peleadores y contenido multimedia, gestionar entradas y administrar eventos y localidades.

Construida con ASP.NET Core 10, Blazor, Entity Framework Core y SQL Server.

## Estado del proyecto

Proyecto en desarrollo.

- Sitio público y panel administrativo implementados.
- Administración protegida con ASP.NET Core Identity, roles y MFA.
- Reservas temporales, conciliación de pagos y emisión de tickets implementadas, con validaciones de integración pendientes.
- Checkout y recepción de eventos Wompi deshabilitados por defecto.
- La preparación para producción y la recuperación administrativa de MFA requieren revisión.

Consulta `Documentacion/` para conocer las pruebas realizadas, restricciones y pendientes de cada entrega.

## Tecnologías

- .NET 10 / ASP.NET Core.
- Blazor.
- Entity Framework Core.
- SQL Server y LocalDB para pruebas locales.
- ASP.NET Core Identity.
- Data Protection para autorización por compra.
- Wompi Sandbox.
- Tickets PDF y códigos QR firmados.

## Funcionalidades

### Sitio público

- Portada y cartelera dinámica.
- Detalle de eventos.
- Directorio de peleadores.
- Galería multimedia.
- Contacto y preguntas frecuentes.
- Consulta pública de QR con validación de autenticidad.

### Compras y entradas

- Validación del comprador y asistentes.
- Precios calculados desde el servidor.
- Compras pendientes y reservas temporales.
- Verificación de pagos desde el backend.
- Descargas PDF y QR protegidas por autorización de compra.

### Administración

- Login privado sin registro público de administradores.
- Política de autorización `AdministracionUGP`.
- MFA obligatorio fuera de Development.
- Gestión de eventos.
- Catálogo de localidades compartidas y asociación con eventos.
- Validaciones para proteger capacidad e inventario.

Las localidades conservan el modelo compartido existente. No debe suponerse que su precio o capacidad son independientes por evento.

## Estructura

| Directorio | Contenido |
|---|---|
| `Components/` | Páginas, layouts y componentes Blazor |
| `Controllers/` | Endpoints HTTP |
| `Data/` | Contexto y configuración de EF Core |
| `Models/` | Entidades y modelos de formularios |
| `Services/` | Lógica de negocio y administración |
| `Sql/` | Scripts y herramientas de gobernanza SQL |
| `Tests/Reservas/` | Comprobaciones aisladas y de integración |
| `wwwroot/` | Recursos públicos |
| `Documentacion/` | Informes, decisiones y evidencias de validación |

## Requisitos locales

- SDK de .NET 10.
- SQL Server con un esquema compatible con la versión del código.
- En Windows, LocalDB para el entorno de integración documentado.
- Certificado HTTPS de desarrollo.
- Configuración privada del entorno.

Clonar el repositorio no copia la base de datos, las cuentas administrativas ni los User Secrets de otro equipo.

## Configuración privada

El proyecto utiliza el identificador de User Secrets definido en su archivo `.csproj`.

Configura los valores privados mediante User Secrets en Development o mediante un gestor de secretos y variables de entorno en el despliegue.

No incluyas valores reales en archivos versionados, capturas, logs o documentación.

### Claves relevantes

| Clave | Propósito |
|---|---|
| `ConnectionStrings:DefaultConnection` | Conexión SQL del entorno |
| `TicketSecurity:SecretKey` | Firma y validación de QR |
| `Administracion:RolPermitido` | Rol exigido para administrar |
| `Administracion:ExigirMfa` | Exigir MFA también en Development |
| `Wompi:Environment` | Entorno de la pasarela |
| `Wompi:PublicKey` | Llave pública del comercio |
| `Wompi:PrivateKey` | Consulta privada de transacciones |
| `Wompi:IntegritySecret` | Firma de integridad del checkout |
| `Wompi:EventsSecret` | Validación de eventos Wompi |
| `Reservas:Habilitadas` | Habilitación del flujo de reservas |
| `Wompi:Webhooks:Habilitados` | Habilitación del receptor de eventos |

Las variables de entorno utilizan doble guion bajo, por ejemplo:
`ConnectionStrings__DefaultConnection`.

Configurar `Administracion:RolPermitido` no crea el rol ni asigna permisos a una cuenta. El usuario debe existir en la base correspondiente y pertenecer al rol mediante Identity.

No generes ni reemplaces automáticamente la clave QR: puede invalidar boletos existentes.

## Base de datos

El entorno aislado utilizado en las pruebas locales es:

- Instancia: `(localdb)\MSSQLLocalDB`.
- Base: `UGP_Integracion_Test`.
- Autenticación: Windows integrada.

LocalDB pertenece al equipo donde se ejecuta. Ese nombre de conexión no permite acceder automáticamente a la base de otro computador.

Revisa los scripts y la documentación antes de inicializar una base:

- `Sql/Gobernanza.sql`
- `Sql/manifest.json`
- `Sql/run.ps1`
- `Sql/UGP_0002_ReservasPagos.sql`
- `Sql/UGP_0003_AdopcionReservasHistoricas.sql`

`UGP_0003` requiere un procedimiento independiente de adopción histórica y permanece bloqueado hasta su aprobación.

No ejecutes migraciones ni scripts automáticamente como parte del arranque. La aplicación de cambios SQL exige verificar destino, respaldo y autorización.

## Compilación y ejecución

Desde la carpeta del proyecto:

```powershell
dotnet restore
dotnet build
dotnet dev-certs https --trust
```

Después de preparar la base y la configuración privada:

```powershell
dotnet run --launch-profile https
```

Consulta `Properties/launchSettings.json` y la salida de la consola para conocer las URL efectivas.

### Administración en integración

El perfil `admin-integracion` fue preparado para el host aislado:

```powershell
dotnet run --launch-profile admin-integracion
```

URL configurada:

```text
https://localhost:7197/acceso-ugp
```

Este perfil utiliza la base de integración, exige MFA y mantiene deshabilitados reservas y webhooks. Revisa su configuración antes de utilizarlo en otro equipo; no es un perfil de producción.

El puerto no concede permisos. El acceso depende de la base conectada, la cuenta, el rol y MFA.

## Acceso administrativo

Rutas principales:

- `/acceso-ugp`
- `/administracion`
- `/administracion/eventos`
- `/administracion/localidades`

La ruta de login es configurable mediante `Administracion:RutaAcceso`.

No existen credenciales administrativas predeterminadas. El provisionamiento debe realizarse mediante las herramientas autorizadas de Identity y sobre el destino verificado.

Nunca deshabilites MFA ni la autorización para resolver un fallo de acceso.

## Wompi

La integración utiliza checkout oficial, firma de integridad y consulta de transacciones desde el servidor.

Un retorno del navegador o la presencia de un ID de transacción no equivalen a un pago aprobado.

Antes de habilitar Sandbox deben verificarse:

- Credenciales auténticas del mismo comercio y entorno.
- Esquema SQL compatible.
- Reservas, expiración e idempotencia.
- Validación criptográfica de webhooks.
- Conciliación e inventario.
- Tratamiento de pagos tardíos.

No habilites producción basándote únicamente en pruebas sintéticas o en una respuesta HTTP 200.

## Pruebas

Compilar el ejecutable de comprobaciones:

```powershell
dotnet build Tests/Reservas/Reservas.Checks.csproj
```

Ejecutar suites aisladas:

```powershell
dotnet run --project Tests/Reservas/Reservas.Checks.csproj -- --isolated
dotnet run --project Tests/Reservas/Reservas.Checks.csproj -- --gallery
dotnet run --project Tests/Reservas/Reservas.Checks.csproj -- --admin-http
```

Las pruebas HTTP requieren permisos para escuchar y conectar a puertos locales.

Las suites SQL, provisionamiento y pruebas CRUD pueden escribir datos. Revísalas antes de ejecutarlas y utiliza exclusivamente una base aislada autorizada.

Comprobaciones Git:

```powershell
git diff --check
git diff --cached --check
```

Una compilación correcta no sustituye las pruebas reales de SQL, navegador, autorización o pagos.

## Seguridad y publicación

Antes de publicar:

1. Resolver todos los marcadores de conflicto.
2. Revisar por separado el directorio de trabajo y el índice.
3. Compilar y ejecutar las pruebas pertinentes.
4. Escanear secretos en archivos actuales y en el historial.
5. Excluir archivos generados, credenciales y respaldos.
6. Revisar los cambios preparados antes del commit.

`.gitignore` no elimina archivos previamente versionados ni secretos presentes en commits anteriores.

No publiques:

- User Secrets.
- Contraseñas o semillas TOTP.
- Códigos de recuperación MFA.
- Llaves privadas y secretos Wompi.
- Claves de firma QR.
- Certificados privados.
- Copias de bases de datos.
- Cookies o credenciales de sesión.
- Directorios `bin/`, `obj/` y `artifacts/`.

## Documentación

Los informes de `Documentacion/` distinguen pruebas reales, pruebas aisladas, revisiones estáticas y escenarios pendientes.

Antes de desplegar, revisa especialmente la documentación de gobernanza SQL, inventario y reservas, Wompi Sandbox y acceso administrativo.