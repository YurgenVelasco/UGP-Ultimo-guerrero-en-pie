# Validación auténtica Wompi Sandbox

Fecha: 2026-11-11. **DICTAMEN: BLOQUEADA POR CONFIGURACIÓN; NO HABILITAR CHECKOUT.**

## Precondiciones comprobadas

- Conexión de solo lectura del runner a `(localdb)\MSSQLLocalDB`, base `UGP_Integracion_Test`: correcta.
- Status: baseline y UGP_0002 aplicados con hashes del manifiesto. No se reportaron versiones desconocidas; UGP_0003 sigue excluido y no se ejecutó. Esto acredita el historial del runner, no descarta operaciones manuales externas.
- Configuración local revisada: Sandbox, Reservas:Habilitadas false y Wompi:Webhooks:Habilitados false.
- Fuentes revisadas: appsettings, appsettings.Development, User Secrets del proyecto y variables relevantes de esta sesión. No se inspeccionó la memoria/configuración de otros procesos abiertos.
- Los fixtures de las entregas anteriores están documentados como sintéticos. No se leyeron datos personales ni se hizo un nuevo censo de toda la base; no se crearon transacciones ni nuevas compras en esta entrega.

## Credenciales, verificadas por separado

| Configuración | Presencia / formato | Autenticidad y comercio |
|---|---|---|
| Wompi:PublicKey | Presente, formato Sandbox | Pendiente de verificación contra proveedor |
| Wompi:PrivateKey | Presente, formato Sandbox | Pendiente de verificación contra proveedor |
| Wompi:IntegritySecret | Presente, formato Sandbox | Pendiente de verificación contra proveedor |
| Wompi:EventsSecret | **Ausente** | Bloquea autenticación de eventos reales |

No se imprimieron ni modificaron valores. Los prefijos no acreditan pertenencia al mismo comercio. No se afirma que las credenciales sean válidas por tener formato correcto.

## Acción del responsable

Seleccionar Sandbox en el Dashboard del comercio Wompi correcto y obtener su secreto de eventos en las opciones de integración técnica/Desarrolladores. Configurarlo privadamente como **Wompi:EventsSecret** en User Secrets de este proyecto, no en appsettings ni en el chat. Referencia ya consultada: [eventos oficiales Colombia](https://docs.wompi.co/en/docs/colombia/eventos/).

Desde una terminal privada en la raíz del proyecto, la entrada enmascarada evita incluir el valor en el comando guardado en historial:

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

No ejecutado por el agente. User Secrets es para desarrollo local; no constituye almacenamiento cifrado de producción. No generar un valor de ejemplo para superar la validación. Informar solamente que fue configurado, sin enviar el secreto. No cambiar flags: los nombres implementados son Reservas:Habilitadas y Wompi:Webhooks:Habilitados, no Reservas ni Wompi:Webhooks como booleanos simples.

## Infraestructura y pruebas auténticas

No se levantó host, no se abrió túnel, no se publicó endpoint ni se configuró URL en Wompi. Se detuvieron las pruebas dependientes del secreto conforme a la condición de parada del usuario.

| Escenario | Resultado |
|---|---|
| Mismo comercio para las cuatro credenciales | PENDIENTE |
| HTTPS público exclusivo para eventos | NO EJECUTADO |
| Crear transacción Sandbox auténtica y persistir ID/referencia | NO EJECUTADO |
| Recibir transaction.updated auténtico y validar firma | BLOQUEADO por secreto ausente |
| Persistencia de evento real y consulta backend | PENDIENTE |
| Aprobación / rechazo / pendiente reales Sandbox | PENDIENTE |
| Conciliación idempotente con eventos auténticos | PENDIENTE |
| Reinicio del host con trabajo auténtico | PENDIENTE |
| Integridad de inventario/tickets con Wompi auténtico | PENDIENTE |

IDs de transacciones reales: ninguno. Respuestas HTTP del proveedor: ninguna. Evidencia SQL nueva limitada a estado de versiones; no hubo escritura ni nueva conciliación. Los resultados sintéticos de los informes anteriores no se presentan como resultados auténticos.

## Validaciones locales y cierre

Compilación de aplicación y harness: cero errores y advertencias en artifacts/wompi-autentica-preflight. Se ejecutaron las 30 comprobaciones aisladas del harness, sin SQL ni llamadas a Wompi. git diff --check sin errores.

No hubo flags temporales que restaurar: ningún host se inició y ningún flag persistido se cambió. No se modificaron credenciales, configuración principal, código, esquema ni bases; únicamente se creó este informe, además de salidas de compilación ignoradas. Sin UGP_0003, migraciones, pagos, commit ni push. HEAD 62bdf1d; cambios locales anteriores preservados, índice sin cambios preparados.

## Riesgos y dictamen

Persisten las pruebas de deadlock y COMMIT incierto, además de toda validación real con el comercio. La cobertura de desconexión controlada y pruebas sintéticas anteriores no resuelve esos riesgos.

**Mantener checkout y receptor público deshabilitados.** Se necesita intervención del responsable para configurar el secreto auténtico antes de continuar la fase autorizada. La activación del checkout exige una autorización posterior separada, incluso después de pasar las pruebas.
