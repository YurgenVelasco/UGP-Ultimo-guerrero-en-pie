using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using UGP_ultimo_guerrero_en_pie_.Data;
using UGP_ultimo_guerrero_en_pie_.Models.ViewModels;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;
using UGP_ultimo_guerrero_en_pie_.Services.Administration;

// SQL real; autorización sustituida exclusivamente en este ejecutable de pruebas.
internal static class AdminCatalogChecks
{
    public static async Task Run()
    {
        if (!await AdminIntegrationPreflight.Run()) throw new Exception("Precondiciones incompletas");
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(AdminIntegrationPreflight.Connection).Options);
        var auth = new CatalogAuthorization();
        var service = new AdminCatalogService(db, new AdminOperations(auth, NullLogger<AdminOperations>.Instance), new EphemeralDataProtectionProvider(), new FixtureHost(), NullLogger<AdminCatalogService>.Instance);
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,"synthetic-admin63")], "fixture"));
        var prefix = "QA63-"+Guid.NewGuid().ToString("N")[..10];
        void Check(bool value,string name) { if(!value) throw new Exception("FALLA: "+name); Console.WriteLine("PASA: "+name); }
        async Task Reject(Func<Task> action,string name) { try { await action(); } catch(ValidationException) { Check(true,name); return; } throw new Exception("FALLA: "+name); }
        AdminEventForm Event(string suffix)=>new(){Nombre=prefix+suffix,Lugar="Laboratorio sintético",Fecha=new DateOnly(2030,1,1),Hora=new TimeOnly(18,0),CapacidadMaxima=20,Estado="Programado"};
        var a=await service.SaveEventAsync(user,0,Event("A"));
        var b=await service.SaveEventAsync(user,0,Event("B"));
        Check(a!=b,"Crear dos eventos sintéticos");
        var form=(await service.EventAsync(user,a))!.Value.Form;
        var stale=(await service.EventAsync(user,a))!.Value.Form;
        form.Lugar="Laboratorio editado";
        await service.SaveEventAsync(user,a,form);
        Check((await service.EventAsync(user,a))!.Value.Event.Lugar==form.Lugar,"Editar y consultar evento");
        await Reject(async()=>{await service.SaveEventAsync(user,a,stale);},"Edición obsoleta rechazada");
        await Reject(async()=>{await service.SaveEventAsync(user,0,Event("A"));},"Reenvío de creación rechazado");
        var type=await service.SaveLocalityAsync(user,0,new(){Nombre=prefix,Precio=10000,Capacidad=10});
        await service.AssociateAsync(user,a,type); await service.AssociateAsync(user,b,type); await service.AssociateAsync(user,a,type);
        Check(await db.EventoTiposEntrada.CountAsync(x=>x.TipoEntradaId==type)==2,"Localidad compartida y asociación idempotente");
        var shared=(await service.LocalityAsync(user,type))!; shared.Precio=20000;
        await Reject(async()=>{await service.SaveLocalityAsync(user,type,shared);},"Precio compartido no editable");
        Check((await service.AvailabilityAsync(user,a)).Single().Precio==10000 && (await service.AvailabilityAsync(user,b)).Single().Precio==10000,"Ambos eventos conservan precio original");
        await Reject(async()=>{await service.SaveLocalityAsync(user,0,new(){Nombre=prefix,Precio=10000,Capacidad=1});},"Nombre global duplicado rechazado");
        var large=await service.SaveLocalityAsync(user,0,new(){Nombre=prefix+"Grande",Precio=10000,Capacidad=15});
        await Reject(()=>service.AssociateAsync(user,a,large),"Suma de capacidades superior al aforo rechazada");
        Check(!await db.EventoTiposEntrada.AnyAsync(x=>x.EventoId==a && x.TipoEntradaId==large),"Asociación fallida sin efectos parciales");
        Check((await service.EventAsync(user,a))!.Value.Event.TicketsDisponibles==20,"Administración no descuenta inventario");
        var exclusive=await service.SaveLocalityAsync(user,0,new(){Nombre=prefix+"Exclusiva",Precio=10000,Capacidad=10});
        await service.AssociateAsync(user,a,exclusive);
        // Fixture explícito, no compra comercial ni confirmación de pago. Sólo en la base literal aislada.
        db.ChangeTracker.Clear();
        var purchase=new Compra {EventoId=a,Visitante=new Visitante {Nombre="QA sintético",Correo=prefix+"@example.invalid"},
            MetodoPago=new MetodoPago {Nombre=prefix},FechaCompra=DateTime.UtcNow,MontoTotal=50000,EstadoPago="Pendiente"};
        purchase.DetalleCompras.Add(new DetalleCompra {TipoEntradaId=exclusive,Cantidad=5,PrecioUnitario=10000,Subtotal=50000});
        await using(var tx=await db.Database.BeginTransactionAsync())
        {
            db.Compras.Add(purchase);
            (await db.Eventos.SingleAsync(e=>e.EventoId==a)).TicketsDisponibles=15;
            await db.SaveChangesAsync(); await tx.CommitAsync();
        }
        var committed=(await service.LocalityAsync(user,exclusive))!; committed.Capacidad=4;
        await Reject(async()=>{await service.SaveLocalityAsync(user,exclusive,committed);},"Capacidad inferior a cinco compromisos rechazada");
        committed=(await service.LocalityAsync(user,exclusive))!; committed.Precio=11000;
        await Reject(async()=>{await service.SaveLocalityAsync(user,exclusive,committed);},"Precio con compra pendiente rechazado");
        var availability=(await service.AvailabilityAsync(user,a)).Single(x=>x.Id==exclusive);
        Check(availability.Reservados==5 && availability.Disponibles==5,"Disponibilidad refleja compromisos SQL");
        auth.Allowed=false;
        try { await service.SaveEventAsync(user,0,Event("Denied")); throw new Exception("Autorización omitida"); }
        catch(UnauthorizedAccessException) { Check(true,"Guardia de servicio rechaza usuario no autorizado"); }
        Check(!await db.Eventos.AnyAsync(x=>x.Nombre==prefix+"Denied"),"Operación denegada no persiste");
        Console.WriteLine($"Evidencia SQL sintética conservada: eventos {a}, {b}; localidades {type}, {large}, {exclusive}; compra fixture {purchase.CompraId}. No se generaron tickets ni pagos.");
    }
    private sealed class CatalogAuthorization : IAuthorizationService
    {
        public bool Allowed=true;
        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user,object? resource,IEnumerable<IAuthorizationRequirement> requirements)=>Task.FromResult(Allowed?AuthorizationResult.Success():AuthorizationResult.Failed());
        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user,object? resource,string policyName)=>Task.FromResult(Allowed && policyName==AdminSettings.Policy?AuthorizationResult.Success():AuthorizationResult.Failed());
    }
}
