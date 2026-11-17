# Validación de integración real Wompi Sandbox

Fecha: 2026-11-11. Dictamen: **MANTENER CHECKOUT BLOQUEADO; integración real pendiente**.

## Configuración verificada sin revelar valores

Se leyeron appsettings.json, appsettings.Development.json, User Secrets del UserSecretsId del proyecto y las variables pertinentes heredadas por la sesión de herramientas. No se imprimieron valores privados ni se cambiaron credenciales. Esta revisión no inspecciona las sobrescrituras o argumentos de un proceso web ya iniciado.

| Configuración | Resultado |
|---|---|
| UserSecretsId | Presente |
| Entorno Wompi | Sandbox según configuración revisada |
| API utilizada por GetPaymentAsync | https://sandbox.wompi.co/v1/transactions/{id} |
| Wompi:PublicKey | Presente, formato Sandbox esperado |
| Wompi:PrivateKey | Presente, formato Sandbox esperado |
| Wompi:IntegritySecret | Presente, formato Sandbox esperado |
| Wompi:EventsSecret | AUSENTE |
| Reservas:Habilitadas | False efectivo en las fuentes revisadas |

El formato no demuestra vigencia de credenciales ni pertenencia al mismo comercio. No se autenticó contra Wompi ni se realizó una operación financiera. La selección de host en código separa Sandbox/Production, pero no constituye una certificación integral de despliegue o de las credenciales de un proceso existente.

La URL coincide con la [documentación oficial de ambientes y llaves](https://docs.wompi.co/docs/colombia/ambientes-y-llaves/).

## Intervención del responsable del comercio

Entrar al Dashboard de Comercios de Wompi, seleccionar Sandbox y consultar el secreto de eventos dentro de las opciones de integración técnica/Desarrolladores del comercio correcto. Registrar también una URL HTTPS de eventos específica para Sandbox; no reutilizar la de producción. Ver [eventos Colombia](https://docs.wompi.co/en/docs/colombia/eventos/) y [orientación oficial sobre las llaves de pruebas](https://soporte.wompi.co/hc/es-419/articles/1500010231462-No-me-aparece-el-bot%C3%B3n-de-Wompi-al-momento-de-pagar-en-mi-tienda-de-Woocommerce).

El responsable debe establecer la clave de configuración **Wompi:EventsSecret** en User Secrets del proyecto local, sin enviarla por chat ni incluirla en appsettings, documentación o Git. Puede usar una terminal privada desde la raíz:

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

No se ejecutó este comando. User Secrets es almacenamiento local para desarrollo, no cifrado de producción. No usar valores inventados ni activar reservas por el solo hecho de configurar el secreto.

## Revisión del webhook (estática, no prueba HTTP)

`Controllers/WompiEventosController.cs` expone POST `/pagos/wompi/eventos`, permite acceso anónimo porque autentica el mensaje, limita el cuerpo a 32768 bytes y exige Request.IsHttps. Retorna 503 si reservas están deshabilitadas o falta el secreto; después valida autenticidad y devuelve 401 ante fallo. Persiste la recepción antes de responder 200.

`WompiCheckout.ValidateEvent` exige transaction.updated, ambiente test en Sandbox y checksum comparado en tiempo constante con SHA-256 de propiedades, timestamp y secreto. El controlador no emite tickets directamente: encola mediante ReservasService; el proceso de conciliación consulta la API y coteja la orden. La idempotencia SQL y la ausencia de tickets por PENDING tuvieron pruebas anteriores con proveedor simulado, no con Wompi real.

Los perfiles revisados publican localhost:7065 HTTPS y localhost:5145 HTTP; eso no proporciona una dirección accesible desde Wompi. No se creó túnel, no se configuró URL en el comercio y no se verificó un endpoint público existente. Program.cs no configura explícitamente ForwardedHeaders: si un futuro proxy termina TLS, debe verificarse cómo preserva HTTPS con proxies confiables, sin aceptar encabezados arbitrarios.

**Acoplamiento pendiente:** el mismo flag Reservas:Habilitadas controla creación de compras, webhook y worker. Mantenerlo false implica 503 para eventos. Antes de la prueba real se necesita un host de prueba aislado y una exposición limitada exclusivamente al webhook, con acceso público al checkout impedido independientemente del flag. No activar el flag en la aplicación habitual ni publicar toda la aplicación mediante un túnel. El despliegue de prueba debe usar exclusivamente UGP_Integracion_Test; no exponer SQL, runner ni archivos privados. Este montaje no se implementó porque falta el secreto auténtico.

## Matriz de pruebas reales solicitadas

| Caso | Resultado | Evidencia/límite |
|---|---|---|
| Configuración y URL API | VERIFICACIÓN LOCAL | API Sandbox correcta; tres formatos válidos; secreto de eventos ausente |
| Accesibilidad HTTPS desde Wompi | PENDIENTE | No hay URL pública validada ni túnel creado |
| Webhook auténtico transaction.updated | PENDIENTE | Falta secreto y receptor operativo |
| Firma inválida por HTTP | PENDIENTE | Rechazo presente en código; con flag false responde antes con 503 |
| Creación y referencia de transacción real Sandbox | PENDIENTE | No se creó transacción para evitar un flujo sin receptor preparado |
| Consulta backend de transacción real | PENDIENTE | Sin ID real verificado |
| Aprobada / rechazada | PENDIENTE | Sin transacciones reales Sandbox |
| Notificación auténtica duplicada | PENDIENTE | Solo evidencia anterior de fixtures SQL |
| Abandono del navegador | PENDIENTE | Sin sesión real de checkout |
| PENDING al vencimiento | PENDIENTE | SQL con fixture aprobado previamente; proveedor real sin probar |
| Confirmación tardía / incidencia | PENDIENTE | SQL con fixture aprobado previamente; proveedor real sin probar |
| Conciliación después de reinicio del host | PENDIENTE | Prueba previa de nuevo proceso no equivale a reinicio del host/worker con eventos reales |
| Sobreventa, unicidad, reserva liberada | PENDIENTE CON WOMPI REAL | Evidencia SQL previa conservada en Validacion-Checkout-Sandbox-UGP.md |

No hay identificadores de transacciones reales ni evidencia de webhooks auténticos en esta entrega. No se presentan mensajes de prueba firmados con secretos ficticios como comunicaciones del proveedor.

## Riesgos, Git y dictamen

Se detuvieron las pruebas dependientes del secreto según la instrucción del usuario. No se inició la aplicación, no se ejecutó SQL ni se modificó la base principal/aislada, no se ejecutó UGP_0003 y no se modificaron flags. No hubo pagos ni cambios de credenciales. No se repitió compilación porque solo se creó este documento; no se atribuyen resultados nuevos de ejecución al código.

Rama main, un commit local por delante de origin/main según referencia local. Se preservan cambios pendientes de etapas anteriores; único archivo creado en esta entrega: este informe. Sin add, commit ni push.

**No habilitar todavía Sandbox público.** Primero el responsable configura el secreto auténtico de eventos; después se prepara el receptor HTTPS aislado con checkout público inaccesible y se ejecutan los casos reales pendientes. La activación del checkout requiere autorización posterior explícita.
