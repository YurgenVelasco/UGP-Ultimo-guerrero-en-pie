using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UGP_ultimo_guerrero_en_pie_.Controllers;
using UGP_ultimo_guerrero_en_pie_.Data;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;
using UGP_ultimo_guerrero_en_pie_.Models.ViewModels;
using UGP_ultimo_guerrero_en_pie_.Services.Implementations;

// Ejecutable separado; no se carga en UGP. Ninguna llamada HTTP sale del proceso.
if (args.Contains("--admin-catalog")) { await AdminCatalogChecks.Run(); return; }
if (args.Contains("--admin-concurrency")) { await AdminConcurrencyChecks.Run(); return; }
if (args.Contains("--admin-catalog-https")) { await AdminIntegrationLogin.Run(catalogChecks:true); return; }
if (args.Contains("--admin-inventory")) { await AdminInventory.Run(); return; }
if (args.Contains("--admin-integration-preflight")) { if (!await AdminIntegrationPreflight.Run()) Environment.ExitCode = 2; return; }
if (args.Contains("--admin-integration-provision"))
{
    try { await AdminIntegrationProvision.Run(); }
    catch (Exception ex) { Console.WriteLine("PROVISIONAMIENTO DETENIDO: " + ex.GetType().Name + ". Etapa: " + AdminIntegrationProvision.Stage + ". Sin datos privados; verifica estado SQL antes de reintentar."); Environment.ExitCode = 2; }
    return;
}
if (args.Contains("--admin-integration-mfa"))
{
    try { await AdminIntegrationMfa.Run(); }
    catch (Exception ex) { Console.WriteLine("MFA DETENIDO: " + ex.GetType().Name + ". No se imprimen secretos."); Environment.ExitCode = 2; }
    return;
}
if (args.Contains("--admin-integration-login") || args.Contains("--admin-integration-login-mfa") || args.Contains("--admin-integration-expiry"))
{
    try { await AdminIntegrationLogin.Run(args.Contains("--admin-integration-login-mfa"), args.Contains("--admin-integration-expiry")); }
    catch (Exception ex) { Console.WriteLine("PRUEBA HTTPS DETENIDA: " + ex.GetType().Name + ". Sin registro de credenciales."); Environment.ExitCode = 2; }
    return;
}
if (args.Contains("--admin-http")) { await AdminHttpChecks.Run(); return; }
if (args.Contains("--gallery")) { await GalleryChecks.Run(); return; }
if (args.Contains("--http-sql")) { await WebhookHttpChecks.Run(); return; }
var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Wompi:Environment"] = "Sandbox", ["Wompi:PublicKey"] = "pub_test_FIXTURE_ONLY",
    ["Wompi:PrivateKey"] = "prv_test_FIXTURE_ONLY", ["Wompi:IntegritySecret"] = "test_integrity_FIXTURE_ONLY",
    ["Wompi:EventsSecret"] = "test_events_FIXTURE_ONLY", ["Reservas:Habilitadas"] = "true",
    ["TicketSecurity:SecretKey"] = "FIXTURE_ONLY_NOT_AN_APPLICATION_KEY"
}).Build();
var provider = new ProviderFixture();
var wompi = new WompiCheckout(config, provider);
void Check(bool condition, string name) { if (!condition) throw new Exception("FALLA: " + name); Console.WriteLine("PASA: " + name); }
string Hash(string input) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
var expiration = "2026-11-10T20:15:00.000Z";
Check(wompi.Sign("UGP-test", 15000, expiration) == Hash("UGP-test15000COP" + expiration + "test_integrity_FIXTURE_ONLY"), "firma con expiración");
Check(wompi.Sign("UGP-test", 15000) == Hash("UGP-test15000COPtest_integrity_FIXTURE_ONLY"), "firma anterior sin expiración");
string EventBody(string status, string checksum, string environment = "test") => JsonSerializer.Serialize(new
{
    @event = "transaction.updated", environment, timestamp = 123456L,
    data = new { transaction = new { id = "fixture-001", status, amount_in_cents = 15000, reference = "UGP-test", currency = "COP" } },
    signature = new { properties = new[] { "transaction.id", "transaction.status", "transaction.amount_in_cents" }, checksum }
});
var checksum = Hash("fixture-001APPROVED15000123456test_events_FIXTURE_ONLY");
using var authentic = JsonDocument.Parse(EventBody("APPROVED", checksum));
Check(wompi.ValidateEvent(authentic.RootElement, out _, out var eventId) && eventId == "fixture-001", "webhook auténtico de fixture");
using var changed = JsonDocument.Parse(EventBody("PENDING", checksum));
Check(!wompi.ValidateEvent(changed.RootElement, out _, out _), "webhook modificado rechazado");
using var otherEnv = JsonDocument.Parse(EventBody("APPROVED", checksum, "prod"));
Check(!wompi.ValidateEvent(otherEnv.RootElement, out _, out _), "webhook de otro entorno rechazado");
using var malformed = JsonDocument.Parse("{}");
Check(!wompi.ValidateEvent(malformed.RootElement, out _, out _), "webhook mal formado rechazado");
Check(!WompiCheckout.ValidTransactionId("../otra") && !WompiCheckout.ValidTransactionId(new string('a',101)), "ID de transacción inválido");
Check(!new WompiCheckout(new ConfigurationBuilder().Build(), provider).ReservationsEnabled, "activación deshabilitada por defecto");
var webhookConfig = new ConfigurationBuilder().AddInMemoryCollection(config.AsEnumerable()).Build();
webhookConfig["Reservas:Habilitadas"] = "false";
webhookConfig["ConnectionStrings:DefaultConnection"] = @"Server=(localdb)\MSSQLLocalDB;Database=UGP_Integracion_Test;Integrated Security=true";
var webhookWompi = new WompiCheckout(webhookConfig, provider);
var host = new FixtureHost();
var policy = new WompiWebhookPolicy(webhookConfig, host, webhookWompi);
var controller = new WompiEventosController(webhookWompi, null!, policy)
{ ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
controller.Request.Scheme = "https";
Check(!policy.Enabled && await controller.Recibir(authentic.RootElement, default) is StatusCodeResult { StatusCode: 503 },
    "receptor deshabilitado por defecto responde 503");
webhookConfig["Wompi:Webhooks:Habilitados"] = "true";
Check(policy.Enabled && !webhookWompi.ReservationsEnabled, "receptor aislado habilitable sin compras (fixture)");
Check(await controller.Recibir(changed.RootElement, default) is UnauthorizedResult, "controlador rechaza firma inválida sin persistir");
Check(await controller.Recibir(otherEnv.RootElement, default) is UnauthorizedResult, "controlador rechaza evento producción");
webhookConfig["Wompi:EventsSecret"] = null;
Check(!policy.Enabled && await controller.Recibir(authentic.RootElement, default) is StatusCodeResult { StatusCode: 503 }, "sin secreto no habilita receptor");
webhookConfig["Wompi:EventsSecret"] = "test_events_FIXTURE_ONLY";
host.EnvironmentName = "Production";
Check(!policy.Enabled, "receptor independiente no habilitable en Production");
host.EnvironmentName = "Development";
webhookConfig["ConnectionStrings:DefaultConnection"] = @"Server=(localdb)\MSSQLLocalDB;Database=UGP_Db_v2;Integrated Security=true";
Check(!policy.Enabled, "receptor rechaza base principal");
webhookConfig["ConnectionStrings:DefaultConnection"] = "Server=remote;Database=UGP_Integracion_Test;Integrated Security=true";
Check(!policy.Enabled, "receptor rechaza servidor remoto");
var momento = new DateTime(2026,11,11,12,0,0,DateTimeKind.Utc);
var cola = new TransaccionPago { Id = Guid.NewGuid(), WompiId = "fixture-queue", Estado = "APPROVED" };
ConciliacionPolicy.Notificar(cola, momento, false);
Check(cola.VersionNotificada == 1 && cola.SiguienteConciliacionUtc == momento && cola.Estado == "APPROVED", "notificación nueva reactiva transacción definitiva sin degradarla");
ConciliacionPolicy.Notificar(cola, momento.AddSeconds(1), true);
Check(cola.VersionNotificada == 1 && cola.SiguienteConciliacionUtc == momento, "recepción duplicada no duplica versión ni altera agenda");
ConciliacionPolicy.Completar(cola, 1, momento, null);
Check(cola.SiguienteConciliacionUtc == null && cola.VersionProcesada == 1, "conciliación consume su generación");
ConciliacionPolicy.Notificar(cola, momento.AddSeconds(2), false);
ConciliacionPolicy.Completar(cola, 1, momento.AddSeconds(3), null);
Check(cola.VersionNotificada == 2 && cola.SiguienteConciliacionUtc == momento.AddSeconds(3), "respuesta antigua no borra notificación recibida durante consulta");
ConciliacionPolicy.Completar(cola, 2, momento.AddSeconds(4), null);
ConciliacionPolicy.Completar(cola, 1, momento.AddSeconds(5), momento.AddHours(1));
Check(cola.VersionProcesada == 2 && cola.SiguienteConciliacionUtc == null && ConciliacionPolicy.EsObsoleta(cola,1), "respuesta fuera de orden no altera decisión más reciente");
Check(ConciliacionPolicy.ConservarEstado("APPROVED","PENDING") && ConciliacionPolicy.ConservarEstado("APPROVED","VOIDED"), "confirmación definitiva no se sobrescribe");
Check(ConciliacionPolicy.ConservarEstado("DECLINED","PENDING") && !ConciliacionPolicy.ConservarEstado("DECLINED","APPROVED"), "rechazo no vuelve a pendiente; aprobación posterior requiere política de reserva");
ConciliacionPolicy.Notificar(cola, momento.AddSeconds(6), false);
ConciliacionPolicy.Completar(cola, 3, momento.AddSeconds(7), momento.AddMinutes(1));
Check(cola.SiguienteConciliacionUtc == momento.AddMinutes(1) && cola.Estado == "APPROVED", "error transitorio reintenta sin convertir pago en rechazo");
await using (var model = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer("Server=unused;Database=unused;Integrated Security=true").Options))
{
    Check(model.Model.FindEntityType(typeof(ReservaCompra))!.FindPrimaryKey()!.Properties.Single().Name == "CompraId",
        "modelo EF reserva única por compra (sin conexión SQL)");
    var query = model.Set<ReservaCompra>().Where(r => r.Estado == "Vigente" && r.ExpiraUtc <= DateTime.UtcNow)
        .Select(r => r.CompraId).Take(100).ToQueryString();
    Check(query.Contains("ReservasCompra") && query.Contains("ExpiraUtc"), "consulta de vencimientos traducible por EF");
    var ddl = model.Database.GenerateCreateScript(); // Genera texto en memoria; NO ejecuta SQL.
    Check(ddl.Contains("[IntentoPagoId], [Entorno]") && ddl.Contains("[Id], [Entorno]"), "EF genera FK compuesta con entorno");
    Check(ddl.Contains("[dbo].[NotificacionesPago]") && ddl.Contains("[dbo].[VerificacionesPago]"), "EF incluye trazabilidad sin conectar SQL");
    Check(model.Model.FindEntityType(typeof(NotificacionPago))!.GetIndexes().Any(i => i.IsUnique &&
        i.Properties.Select(p=>p.Name).SequenceEqual(new[] { "TransaccionPagoId", "Huella" })), "unicidad de recepción configurada en EF");
    Check(ddl.Contains("CK_Transaccion_Version") && ddl.Contains("CK_Reserva_Confirmacion"), "EF conserva restricciones de versión y vencimiento");
}

if (!args.Contains("--sql"))
{
    Console.WriteLine("SQL NO EJECUTADO. Requiere revisión/aplicación manual del esquema en una base desechable.");
    return;
}
var connection = Environment.GetEnvironmentVariable("UGP_RESERVAS_TEST_CONNECTION") ?? throw new Exception("Falta conexión privada de PRUEBAS.");
var builder = new SqlConnectionStringBuilder(connection);
var integrationTarget = builder.DataSource == @"(localdb)\MSSQLLocalDB" && builder.InitialCatalog == "UGP_Integracion_Test";
if ((!integrationTarget && !builder.InitialCatalog.StartsWith("UGP_Reservas_Test_", StringComparison.Ordinal)) ||
    Environment.GetEnvironmentVariable("UGP_RESERVAS_TEST_APPROVED") != "SCHEMA_REVIEWED_DISPOSABLE_DATABASE")
    throw new Exception("Solo se admiten bases desechables UGP_Reservas_Test_* aprobadas. No se crea/aplica esquema.");

ApplicationDbContext Db(DbCommandInterceptor? interceptor = null)
{
    var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection,
        sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), [51011, 51013]));
    if (interceptor != null) options.AddInterceptors(interceptor);
    return new ApplicationDbContext(options.Options);
}
ReservasService Service(ApplicationDbContext db) => new(db, wompi, new TicketService(db, new QrService(config)), NullLogger<ReservasService>.Instance);
if (args.Contains("--expire"))
{
    var id = int.Parse(args[Array.IndexOf(args,"--expire") + 1]);
    await using var child = Db();
    await Service(child).ExpirarAsync(id);
    return;
}
if (args.Contains("--resume-rollback"))
{
    if (!integrationTarget) throw new Exception("Reanudación limitada a la base local autorizada.");
    await using var resumeDb = Db();
    // La bandeja puede persistir sin asociación al intento tras rollback.
    // Esta reanudación puntual usa el último fixture y comprueba su estado antes de escribir.
    var purchaseId = await resumeDb.Compras.OrderByDescending(c => c.CompraId).Select(c => c.CompraId).FirstAsync();
    await AssertState(purchaseId,"Pendiente",0,4,"SQL rollback conservado tras interrupción del harness");
    await SetPayment(purchaseId,"tx-rollback","APPROVED");
    await Verify(purchaseId,"tx-rollback");
    await Verify(purchaseId,"tx-rollback");
    await AssertState(purchaseId,"Aprobado",1,4,"SQL reintento tras rollback emite una vez");
    var recovery = await Reserve(await Fixture()); await Age(recovery);
    await ExpireProcess(recovery);
    await AssertState(recovery,"Expirada",0,5,"SQL proceso nuevo recupera reserva vencida");
    Console.WriteLine("Reanudación SQL terminada; fixtures conservados.");
    return;
}
if (!args.Contains("--worker-sql"))
await using (var preflight = Db())
{
    // Lectura previa: una base con datos existentes no es un fixture limpio.
    if (await preflight.Compras.AnyAsync() || await preflight.Tickets.AnyAsync() || await preflight.Eventos.AnyAsync())
        throw new Exception("La base de pruebas debe estar vacía; no se elimina ningún registro.");
    await preflight.Set<ReservaCompra>().CountAsync();
}

async Task<(int evento, int tipo)> Fixture(int capacity = 5)
{
    await using var db = Db();
    var e = new Evento { Nombre = "Fixture reservas", Fecha = new DateOnly(2099,1,1), Hora = new TimeOnly(18,0), Lugar = "Pruebas",
        EstadoEvento = "Activo", CapacidadMaxima = capacity, TicketsDisponibles = capacity };
    var t = new TipoEntrada { Nombre = "Test-" + Guid.NewGuid().ToString("N"), Precio = 10000, Capacidad = capacity };
    db.Add(new EventoTipoEntrada { Evento = e, TipoEntrada = t });
    await db.SaveChangesAsync();
    return (e.EventoId, t.TipoEntradaId);
}
async Task<int> Reserve((int evento, int tipo) fixture)
{
    await using var db = Db();
    var email = Guid.NewGuid().ToString("N") + "@example.test";
    var form = new CheckoutFormVm { EventoId = fixture.evento, TipoEntradaId = fixture.tipo, Cantidad = 1,
        NombreComprador = "Ana Prueba", CorreoComprador = email, DocumentoComprador = "12345", TelefonoComprador = "3001234567",
        Asistentes = [new() { NombreCompleto = "Ana Prueba", CorreoElectronico = email, DocumentoIdentidad = "12345", Telefono = "3001234567" }] };
    var result = await new CompraService(db, Service(db), wompi, null!).CrearCompraPendienteAsync(form);
    return result.CompraId;
}
async Task Age(int id)
{
    await using var db = Db();
    var utc = DateTime.UtcNow;
    await db.Set<ReservaCompra>().Where(r => r.CompraId == id && r.Estado == "Vigente").ExecuteUpdateAsync(s =>
        s.SetProperty(r => r.CreadaUtc, utc.AddMinutes(-16)).SetProperty(r => r.ExpiraUtc, utc.AddMinutes(-1)));
}
async Task SetPayment(int id, string tx, string status)
{
    await using var db = Db();
    var i = await db.Set<IntentoPago>().SingleAsync(i => i.CompraId == id);
    provider.Payments[tx] = new PagoWompi(tx, i.Referencia, i.Centavos, i.Moneda, status);
}
async Task Verify(int id, string tx, DbCommandInterceptor? fault = null)
{
    await using var db = Db(fault);
    await Service(db).VerificarAsync(tx, id);
}
async Task Expire(int id)
{
    await using var db = Db();
    await Service(db).ExpirarAsync(id);
}
async Task AssertState(int id, string state, int ticketCount, int available, string label)
{
    await using var db = Db();
    var c = await db.Compras.Include(c => c.Evento).SingleAsync(c => c.CompraId == id);
    Check(c.EstadoPago == state && c.Evento.TicketsDisponibles == available &&
        await db.Tickets.CountAsync(t => t.CompraId == id) == ticketCount, label);
}

if (args.Contains("--worker-sql"))
{
    if (!integrationTarget) throw new Exception("Worker limitado al destino aislado exacto.");
    var liveWorker = await Reserve(await Fixture());
    var expiredWorker = await Reserve(await Fixture()); await Age(expiredWorker);
    var pendingWorker = await Reserve(await Fixture());
    var suffix = Guid.NewGuid().ToString("N");
    await SetPayment(pendingWorker,"worker-pending-"+suffix,"PENDING");
    await Verify(pendingWorker,"worker-pending-"+suffix); await Age(pendingWorker);
    var approvedWorker = await Reserve(await Fixture());
    await SetPayment(approvedWorker,"worker-approved-"+suffix,"APPROVED");
    await Verify(approvedWorker,"worker-approved-"+suffix);
    var recoveryWorker = await Reserve(await Fixture());
    // Solo se activó creación en memoria del harness sin servidor HTTP para preparar fixtures.
    config["Reservas:Habilitadas"] = "false";
    config["Wompi:Webhooks:Habilitados"] = "true";
    config["ConnectionStrings:DefaultConnection"] = connection;
    IHost WorkerHost()
    {
        var b = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName="Development" });
        b.Configuration.Sources.Clear(); b.Configuration.AddInMemoryCollection(config.AsEnumerable());
        b.Logging.ClearProviders();
        b.Services.AddSingleton<IHttpClientFactory>(provider);
        b.Services.AddDbContext<ApplicationDbContext>(o=>o.UseSqlServer(connection,s=>s.EnableRetryOnFailure(5,TimeSpan.FromSeconds(10),[51011,51013])));
        b.Services.AddScoped<WompiCheckout>(); b.Services.AddScoped<WompiWebhookPolicy>();
        b.Services.AddScoped<ReservasService>(); b.Services.AddScoped<UGP_ultimo_guerrero_en_pie_.Services.Interfaces.ITicketService,TicketService>();
        b.Services.AddScoped<UGP_ultimo_guerrero_en_pie_.Services.Interfaces.IQrService,QrService>();
        b.Services.AddHostedService<ReservasWorker>(); return b.Build();
    }
    using (var a=WorkerHost()) using(var b=WorkerHost())
    {
        try
        {
            await Task.WhenAll(a.StartAsync(),b.StartAsync());
            for(var n=0;n<40;n++)
            {
                await using var checkDb=Db();
                if(await checkDb.Set<ReservaCompra>().CountAsync(r=>(r.CompraId==expiredWorker || r.CompraId==pendingWorker)&&r.Estado=="Liberada")==2)break;
                await Task.Delay(500);
            }
            await AssertState(liveWorker,"Pendiente",0,4,"worker conserva vigente");
            await AssertState(expiredWorker,"Expirada",0,5,"dos workers descubren y liberan vencida una vez");
            await AssertState(pendingWorker,"Expirada",0,5,"worker expira PENDING sin tickets");
            await AssertState(approvedWorker,"Aprobado",1,4,"worker conserva confirmada y ticket único");
        }
        finally {await Task.WhenAll(a.StopAsync(),b.StopAsync());}
    }
    // Sesión dedicada: no KILL, no cambio global; desconexión durante un comando real sin commit.
    int eventRecovery;
    await using(var rd=Db())eventRecovery=await rd.Compras.Where(c=>c.CompraId==recoveryWorker).Select(c=>c.EventoId).SingleAsync();
    await using(var cut=new SqlConnection(connection+";Pooling=false;Application Name=UGP_Isolated_Disconnect_Test"))
    {
        await cut.OpenAsync(); await using var tx=await cut.BeginTransactionAsync();
        await using var command=cut.CreateCommand();command.Transaction=(SqlTransaction)tx;
        command.CommandText="UPDATE dbo.Eventos SET tickets_disponibles=0 WHERE evento_id=@id;";
        command.Parameters.AddWithValue("@id",eventRecovery); await command.ExecuteNonQueryAsync();
        command.CommandText="WAITFOR DELAY '00:00:20'; SELECT 1;";
        var inFlight=command.ExecuteScalarAsync();await Task.Delay(500);await cut.CloseAsync();
        bool interrupted=false;
        try{await inFlight;}catch(Exception ex) when(ex is SqlException or InvalidOperationException){interrupted=true;Console.WriteLine("Interrupción real: "+ex.GetType().Name);}
        Check(interrupted,"conexión propia interrumpida durante comando SQL real");
    }
    await AssertState(recoveryWorker,"Pendiente",0,4,"desconexión revierte actualización no confirmada");
    await Age(recoveryWorker);
    using(var restartHost=WorkerHost())
    {
        try
        {
            await restartHost.StartAsync();
            for(var n=0;n<40;n++)
            {await using var d=Db();if(await d.Set<ReservaCompra>().AnyAsync(r=>r.CompraId==recoveryWorker&&r.Estado=="Liberada"))break;await Task.Delay(500);}
            await AssertState(recoveryWorker,"Expirada",0,5,"worker reiniciado recupera trabajo persistido tras fallo SQL");
            await Task.Delay(11000);
            await AssertState(recoveryWorker,"Expirada",0,5,"siguiente ciclo periódico no duplica liberación");
            await AssertState(approvedWorker,"Aprobado",1,4,"reinicio no duplica ticket confirmado");
        }
        finally {await restartHost.StopAsync();}
    }
    await using(var integrity=Db())
    {
        Check(!await integrity.Eventos.AnyAsync(e=>e.TicketsDisponibles<0||e.TicketsDisponibles>e.CapacidadMaxima),"integridad global inventario acotado");
        Check(!await integrity.Tickets.AnyAsync(t=>t.Compra.EstadoPago!="Aprobado"),"tickets solo en compras aprobadas");
        Check(!await integrity.Set<ReservaCompra>().AnyAsync(r=>r.Estado=="Liberada"&&r.LiberadaUtc==null),"sin liberaciones incompletas");
    }
    config["Wompi:Webhooks:Habilitados"]="false";
    Console.WriteLine($"Fixtures worker: {liveWorker},{expiredWorker},{pendingWorker},{approvedWorker},{recoveryWorker}. Hosts detenidos, flags temporales false.");
    return;
}
var active = await Reserve(await Fixture());
await Expire(active);
await AssertState(active, "Pendiente", 0, 4, "SQL reserva vigente");
await Age(active);
async Task ExpireProcess(int id)
{
    var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true };
    start.ArgumentList.Add(typeof(ProviderFixture).Assembly.Location);
    start.ArgumentList.Add("--sql"); start.ArgumentList.Add("--expire"); start.ArgumentList.Add(id.ToString());
    using var process = Process.Start(start) ?? throw new Exception("No se inició proceso de prueba");
    await process.WaitForExitAsync();
    Check(process.ExitCode == 0,"SQL proceso independiente finaliza correctamente");
}
await Task.WhenAll(ExpireProcess(active), ExpireProcess(active));
await Expire(active);
await AssertState(active, "Expirada", 0, 5, "SQL expiración concurrente y duplicada; abandono");

var rejected = await Reserve(await Fixture());
await SetPayment(rejected,"tx-rejected","DECLINED");
await Task.WhenAll(Verify(rejected,"tx-rejected"),Verify(rejected,"tx-rejected"));
await AssertState(rejected,"Rechazado",0,5,"SQL rechazo duplicado libera una vez");

var pending = await Reserve(await Fixture());
await SetPayment(pending,"tx-pending","PENDING");
await Verify(pending,"tx-pending");
await Age(pending); await Expire(pending);
await AssertState(pending,"Expirada",0,5,"SQL PENDING al vencimiento no emite");
await SetPayment(pending,"tx-pending","APPROVED");
await Task.WhenAll(Verify(pending,"tx-pending"),Verify(pending,"tx-pending"));
await AssertState(pending,"Expirada",0,5,"SQL aprobación tardía no recupera inventario");
await using (var db = Db()) Check(await db.Set<IncidenciaPago>().CountAsync() == 1,"SQL incidencia tardía única");

var approved = await Reserve(await Fixture());
await SetPayment(approved,"tx-approved","APPROVED");
async Task Notify(string id) { await using var db = Db(); await Service(db).RecibirNotificacionAsync(id,default,Hash(id)); }
await Task.WhenAll(Notify("tx-approved"),Notify("tx-approved"));
await using (var db = Db()) Check(await db.Set<TransaccionPago>().CountAsync(t => t.WompiId == "tx-approved") == 1,
    "SQL webhook duplicado: bandeja única");
await Task.WhenAll(Verify(approved,"tx-approved"),Verify(approved,"tx-approved"));
await AssertState(approved,"Aprobado",1,4,"SQL confirmación simultánea emite una vez");
await using (var db = Db())
{
    await Service(db).RecibirNotificacionAsync("tx-approved",default,Hash("segunda-recepcion"));
    var transaccion = await db.Set<TransaccionPago>().AsNoTracking().SingleAsync(t=>t.WompiId=="tx-approved");
    Check(transaccion.VersionNotificada == 2 && transaccion.SiguienteConciliacionUtc != null && transaccion.Estado == "APPROVED",
        "SQL ID terminal existente se reencola sin perder aprobación");
}
await SetPayment(approved,"tx-approved","PENDING");
await Verify(approved,"tx-approved");
await Age(approved); await Expire(approved);
await AssertState(approved,"Aprobado",1,4,"SQL notificación fuera de orden y expiración no revierten aprobado");

var boundary = await Reserve(await Fixture());
await SetPayment(boundary,"tx-boundary","APPROVED");
await using (var db = Db())
{
    var deadline = DateTime.UtcNow.AddMilliseconds(100);
    await db.Set<ReservaCompra>().Where(r => r.CompraId == boundary).ExecuteUpdateAsync(s =>
        s.SetProperty(r => r.CreadaUtc,deadline.AddMinutes(-15)).SetProperty(r => r.ExpiraUtc,deadline));
}
await Task.WhenAll(Verify(boundary,"tx-boundary"),Expire(boundary));
await Expire(boundary);
await using (var db = Db())
{
    var c = await db.Compras.Include(c=>c.Evento).SingleAsync(c=>c.CompraId == boundary);
    var count = await db.Tickets.CountAsync(t=>t.CompraId == boundary);
    Check((c.EstadoPago == "Aprobado" && count == 1 && c.Evento.TicketsDisponibles == 4) ||
        (c.EstadoPago == "Expirada" && count == 0 && c.Evento.TicketsDisponibles == 5),"SQL carrera aprobación/expiración: un único resultado consistente");
}

var foreign = await Reserve(await Fixture());
await Verify(foreign,"tx-approved");
await AssertState(foreign,"Pendiente",0,4,"SQL transacción ajena no aprueba otra orden");

var abandoned = await Reserve(await Fixture());
await using (var db = Db()) await Service(db).RecibirNotificacionAsync("tx-offline",default);
provider.Offline = true;
await Verify(abandoned,"tx-offline");
await Age(abandoned); await Expire(abandoned);
provider.Offline = false;
await AssertState(abandoned,"Expirada",0,5,"SQL error del proveedor no retiene inventario");

var last = await Fixture(1);
async Task<int?> TryReserve() { try { return await Reserve(last); } catch (ValidationException) { return null; } }
var lastResults = await Task.WhenAll(TryReserve(), TryReserve());
Check(lastResults.Count(x => x.HasValue) == 1,"SQL dos compradores: solo uno obtiene último cupo");
await AssertState(lastResults.Single(x=>x.HasValue)!.Value,"Pendiente",0,0,"SQL último cupo no negativo");

var rollback = await Reserve(await Fixture());
await SetPayment(rollback,"tx-rollback","APPROVED");
try { await Verify(rollback,"tx-rollback",new FailTicketInsert()); throw new Exception("No se inyectó el fallo"); }
catch (InjectedFailure) { }
catch (DbUpdateException ex) when (ex.InnerException is InjectedFailure) { }
await AssertState(rollback,"Pendiente",0,4,"SQL fallo durante emisión revierte confirmación");
await Verify(rollback,"tx-rollback");
await AssertState(rollback,"Aprobado",1,4,"SQL reintento tras rollback emite una vez");

var restart = await Reserve(await Fixture()); await Age(restart);
await ExpireProcess(restart);
await AssertState(restart,"Expirada",0,5,"SQL proceso nuevo recupera reserva vencida");
Console.WriteLine("Pruebas SQL terminadas. Datos sintéticos conservados; no se borró ni modificó esquema. Wompi fue un fixture HTTP en memoria.");

sealed class ProviderFixture : IHttpClientFactory
{
    public ConcurrentDictionary<string, PagoWompi> Payments { get; } = new();
    public bool Offline { get; set; }
    public HttpClient CreateClient(string name) => new(new Handler(this));
    private sealed class Handler(ProviderFixture fixture) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (fixture.Offline) throw new HttpRequestException("Fallo controlado del fixture");
            var id = request.RequestUri!.Segments.Last();
            if (!fixture.Payments.TryGetValue(id, out var p)) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new
                { data = new { id = p.Id, reference = p.Referencia, amount_in_cents = p.Centavos, currency = p.Moneda, status = p.Estado } })) });
        }
    }
}
sealed class InjectedFailure : Exception { }
sealed class FixtureHost : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "Fixture";
    public string ContentRootPath { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
sealed class FailTicketInsert : DbCommandInterceptor
{
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
        InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        if (command.CommandText.Contains("INSERT INTO [Tickets]",StringComparison.Ordinal)) throw new InjectedFailure();
        return ValueTask.FromResult(result);
    }
}
