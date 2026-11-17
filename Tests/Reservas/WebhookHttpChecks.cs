using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UGP_ultimo_guerrero_en_pie_.Controllers;
using UGP_ultimo_guerrero_en_pie_.Data;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;
using UGP_ultimo_guerrero_en_pie_.Services.Implementations;
using UGP_ultimo_guerrero_en_pie_.Services.Interfaces;

internal static class WebhookHttpChecks
{
    const string Connection = @"Server=(localdb)\MSSQLLocalDB;Database=UGP_Integracion_Test;Integrated Security=true;Encrypt=false";
    const string Secret = "test_events_HTTP_SYNTHETIC_ONLY";
    static void Check(bool ok, string name) { if (!ok) throw new Exception("FALLA: " + name); Console.WriteLine("PASA: " + name); }
    static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(Connection).Options);
    public static async Task Run()
    {
        if (Environment.GetEnvironmentVariable("UGP_RESERVAS_TEST_APPROVED") != "SCHEMA_REVIEWED_DISPOSABLE_DATABASE")
            throw new Exception("Falta autorización explícita de la base aislada.");
        await using var db = Db();
        await db.Database.OpenConnectionAsync();
        using (var command = db.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "SELECT COUNT(*) FROM dbo.SchemaVersion WHERE (ScriptId='baseline' AND Checksum='a7fe08d90752e1cf438fede2d574810368973b42d4284e62e289437d999fb3e1') OR (ScriptId='UGP_0002' AND Checksum='9e801b8d855a9ae5f9ad24a7b0a7b961de75988213a3cc06324a6e95ec0b6320')";
            Check(Convert.ToInt32(await command.ExecuteScalarAsync()) == 2,"preflight versiones SQL");
            command.CommandText = "SELECT COUNT(*) FROM dbo.SchemaVersion WHERE ScriptId NOT IN ('baseline','UGP_0002')";
            Check(Convert.ToInt32(await command.ExecuteScalarAsync()) == 0,"sin otras versiones registradas (incluido UGP_0003)");
        }
        var inventory = await db.Eventos.AsNoTracking().OrderBy(e=>e.EventoId).Select(e=>new {e.EventoId,e.TicketsDisponibles}).ToListAsync();
        var tickets = await db.Tickets.CountAsync();
        // El worker de producción expira reservas existentes: no arrancarlo si cambiaría fixtures anteriores.
        var expired = await db.Set<ReservaCompra>().CountAsync(r=>r.Estado=="Vigente" && r.ExpiraUtc <= DateTime.UtcNow);
        var run = "http-" + Guid.NewGuid().ToString("N");
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=localhost",rsa,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);
        using var generated = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1),DateTimeOffset.UtcNow.AddHours(1));
        // Schannel necesita una clave importada; no se instala confianza global.
        using var cert = X509CertificateLoader.LoadPkcs12(generated.Export(X509ContentType.Pfx),null,X509KeyStorageFlags.UserKeySet);
        var failure = new FailureSwitch();
        var cfg = new Dictionary<string,string?>
        {
            ["ConnectionStrings:DefaultConnection"]=Connection,["Reservas:Habilitadas"]="false",
            ["Wompi:Webhooks:Habilitados"]="true",["Wompi:Environment"]="Sandbox",
            ["Wompi:EventsSecret"]=Secret,["Wompi:PublicKey"]="pub_test_HTTP_FIXTURE",
            ["Wompi:PrivateKey"]="prv_test_HTTP_FIXTURE",["Wompi:IntegritySecret"]="test_integrity_HTTP_FIXTURE",
            ["TicketSecurity:SecretKey"]="HTTP_FIXTURE_NOT_A_REAL_QR_KEY"
        };
        async Task<WebApplication> Start(bool worker = false)
        {
            var b=WebApplication.CreateBuilder(new WebApplicationOptions {EnvironmentName="Development"});
            b.Configuration.Sources.Clear(); b.Configuration.AddInMemoryCollection(cfg);
            b.Logging.ClearProviders(); // Sin secretos, payloads ni SQL en logs del host.
            b.WebHost.ConfigureKestrel(k=>k.Listen(IPAddress.Loopback,0,l=>l.UseHttps(cert)));
            b.Services.AddDbContext<ApplicationDbContext>(o=>o.UseSqlServer(Connection).AddInterceptors(failure));
            b.Services.AddSingleton<IHttpClientFactory, NoNetwork>();
            b.Services.AddScoped<WompiCheckout>(); b.Services.AddScoped<WompiWebhookPolicy>();
            b.Services.AddScoped<ReservasService>(); b.Services.AddScoped<ITicketService,TicketService>();
            b.Services.AddScoped<IQrService,QrService>();
            if(worker)b.Services.AddHostedService<ReservasWorker>();
            b.Services.AddControllers().AddApplicationPart(typeof(WompiEventosController).Assembly)
                .ConfigureApplicationPartManager(m=>m.FeatureProviders.Add(new OnlyWebhook()));
            var app=b.Build();
            app.Use(async (ctx,next)=> {try {await next();}catch {ctx.Response.StatusCode=503;} });
            app.MapControllers(); await app.StartAsync(); return app;
        }
        HttpClient Client(WebApplication app)
        {
            var address=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            Console.WriteLine("Host aislado: "+address);
            return new HttpClient(new HttpClientHandler {ServerCertificateCustomValidationCallback=(_,c,_,_)=>c?.GetCertHashString()==cert.GetCertHashString()}) {BaseAddress=new Uri(address)};
        }
        object Event(string id,string state="PENDING",string env="test",bool valid=true)
        {
            var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id+state+"10000123456"+Secret)));
            return new { @event="transaction.updated",environment=env,timestamp=123456,
                data=new {transaction=new {id,status=state,amount_in_cents=10000,reference=run,currency="COP"}},
                signature=new {properties=new[]{"transaction.id","transaction.status","transaction.amount_in_cents"},checksum=valid?hash:new string('0',64)}};
        }
        async Task<int> Send(HttpClient c,object payload) {using var r=await c.PostAsJsonAsync("/pagos/wompi/eventos",payload);return (int)r.StatusCode;}
        var app=await Start();
        try
        {
            using var client=Client(app);
            Check(await Send(client,Event(run,valid:false))==401,"HTTP firma inválida 401");
            Check(await Send(client,Event(run,env:"prod"))==401,"HTTP ambiente incorrecto 401");
            Check(!await db.Set<TransaccionPago>().AnyAsync(t=>t.WompiId==run),"rechazos sin persistencia");
            Check(await Send(client,Event(run))==200,"HTTP sintético válido 200");
            var t=await db.Set<TransaccionPago>().AsNoTracking().SingleAsync(t=>t.WompiId==run);
            Check(await db.Set<NotificacionPago>().CountAsync(n=>n.TransaccionPagoId==t.Id)==1,"persistencia visible tras HTTP 200");
            Check(await Send(client,Event(run))==200 && await db.Set<NotificacionPago>().CountAsync(n=>n.TransaccionPagoId==t.Id)==1,"duplicado HTTP 200 sin fila adicional");
            // Estado terminal sintético solo en bandeja sin compra; no aprueba órdenes.
            await db.Set<TransaccionPago>().Where(x=>x.Id==t.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Estado,"APPROVED").SetProperty(x=>x.SiguienteConciliacionUtc,(DateTime?)null));
            Check(await Send(client,Event(run,"DECLINED"))==200,"evento fuera de orden recibido 200");
            t=await db.Set<TransaccionPago>().AsNoTracking().SingleAsync(x=>x.Id==t.Id);
            Check(t.Estado=="APPROVED" && t.VersionNotificada==2 && t.SiguienteConciliacionUtc!=null,"reencolado sin degradar estado por payload");
            failure.Fail=true;
            Check(await Send(client,Event(run+"-failure"))==503,"fallo de SaveChanges no devuelve 200");
            Check(!await db.Set<TransaccionPago>().AnyAsync(x=>x.WompiId==run+"-failure"),"fallo de persistencia sin fila parcial");
            failure.Fail=false;
            Check(await Send(client,Event(run+"-failure"))==200,"reentrega tras fallo persiste 200");
            foreach(var path in new[]{"/admin","/checkout","/Sql/run.ps1"})
                Check((int)(await client.GetAsync(path)).StatusCode==404,"ruta no expuesta "+path);
        }
        finally {failure.Fail=false;await app.StopAsync();await app.DisposeAsync();}
        app=await Start(worker: expired==0);
        try
        {
            using var client=Client(app);
            using var scope=app.Services.CreateScope();
            Check(!scope.ServiceProvider.GetRequiredService<WompiCheckout>().ReservationsEnabled,"checkout false tras reinicio");
            if(expired==0)
            {
                for(var attempt=0;attempt<30;attempt++)
                {
                    if(await db.Set<TransaccionPago>().AnyAsync(t=>t.WompiId==run && t.UltimoResultado=="SinRespuestaVerificable"))break;
                    await Task.Delay(500);
                }
            }
            else await scope.ServiceProvider.GetRequiredService<ReservasService>().VerificarAsync(run);
            var t=await db.Set<TransaccionPago>().AsNoTracking().SingleAsync(t=>t.WompiId==run);
            Check(t.UltimoResultado=="SinRespuestaVerificable" && t.SiguienteConciliacionUtc!=null,"conciliación tras reinicio con checkout false; proveedor simulado offline");
            Check(await Send(client,Event(run,"DECLINED"))==200 && await db.Set<NotificacionPago>().CountAsync(n=>n.TransaccionPagoId==t.Id)==2,"duplicado tras reinicio mantiene unicidad");
            Console.WriteLine(expired==0 ? "PASA: worker real procesa agenda tras reinicio con checkout false" : "PENDIENTE: worker periódico completo; reservas vencidas preexistentes="+expired);
        }
        finally {await app.StopAsync();await app.DisposeAsync();cfg["Wompi:Webhooks:Habilitados"]="false";}
        var after=await db.Eventos.AsNoTracking().OrderBy(e=>e.EventoId).Select(e=>new {e.EventoId,e.TicketsDisponibles}).ToListAsync();
        Check(inventory.SequenceEqual(after) && tickets==await db.Tickets.CountAsync(),"inventario y tickets intactos");
        Console.WriteLine("Run sintético: "+run+". Hosts detenidos; configuración temporal descartada.");
    }
    sealed class OnlyWebhook : IApplicationFeatureProvider<ControllerFeature>
    { public void PopulateFeature(IEnumerable<ApplicationPart> parts,ControllerFeature feature)
      {for(var i=feature.Controllers.Count-1;i>=0;i--)if(feature.Controllers[i].AsType()!=typeof(WompiEventosController))feature.Controllers.RemoveAt(i);} }
    sealed class FailureSwitch : SaveChangesInterceptor
    { public bool Fail; public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData d,InterceptionResult<int> r,CancellationToken ct=default)
      {if(Fail)throw new IOException("Fallo sintético de persistencia");return ValueTask.FromResult(r);} }
    sealed class NoNetwork : IHttpClientFactory
    {public HttpClient CreateClient(string name)=>new(new Offline());sealed class Offline:HttpMessageHandler
      {protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct)=>throw new HttpRequestException("Proveedor sintético offline");}}
}
