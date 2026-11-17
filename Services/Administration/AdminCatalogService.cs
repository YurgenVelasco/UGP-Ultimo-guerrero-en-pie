using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using UGP_ultimo_guerrero_en_pie_.Components.Multimedia;
using UGP_ultimo_guerrero_en_pie_.Data;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;
using UGP_ultimo_guerrero_en_pie_.Models.ViewModels;
using UGP_ultimo_guerrero_en_pie_.Services.Implementations;

namespace UGP_ultimo_guerrero_en_pie_.Services.Administration;

public sealed record AdminLocalityRow(int Id, string Nombre, decimal Precio, int Capacidad, int Eventos);
public sealed record AdminAvailability(int Id, string Nombre, decimal Precio, int Capacidad, long Reservados, long Vendidos, long Disponibles);

public sealed class AdminCatalogService(ApplicationDbContext db, AdminOperations access, IDataProtectionProvider protection,
    IHostEnvironment environment, ILogger<AdminCatalogService> logger)
{
    public static readonly string[] States = ["Programado", "Próximo", "Activo", "Finalizado", "Cancelado"];
    private readonly IDataProtector protector = protection.CreateProtector("UGP.AdminCatalog.Edit.v1");

    public async Task<List<Evento>> EventsAsync(ClaimsPrincipal user)
    {
        await access.DemandAsync(user);
        return await db.Eventos.AsNoTracking().OrderByDescending(e => e.Fecha).ThenBy(e => e.EventoId).Take(200).ToListAsync();
    }
    public async Task<(Evento Event, AdminEventForm Form)?> EventAsync(ClaimsPrincipal user, int id)
    {
        await access.DemandAsync(user);
        var e = await db.Eventos.AsNoTracking().SingleOrDefaultAsync(e => e.EventoId == id);
        return e is null ? null : (e, new AdminEventForm { Nombre=e.Nombre, Fecha=e.Fecha, Hora=e.Hora, Lugar=e.Lugar,
            Descripcion=e.Descripcion, ImagenUrl=e.ImagenUrl, CapacidadMaxima=e.CapacidadMaxima, Estado=e.EstadoEvento ?? "Programado", Version=protector.Protect(EventVersion(e)) });
    }
    public async Task<List<AdminLocalityRow>> LocalitiesAsync(ClaimsPrincipal user)
    {
        await access.DemandAsync(user);
        return await db.TiposEntrada.AsNoTracking().OrderBy(t=>t.Nombre).Select(t=>new AdminLocalityRow(t.TipoEntradaId,t.Nombre,t.Precio,t.Capacidad,t.EventoTiposEntrada.Count)).Take(500).ToListAsync();
    }
    public async Task<AdminLocalityForm?> LocalityAsync(ClaimsPrincipal user, int id)
    {
        await access.DemandAsync(user);
        var t = await db.TiposEntrada.AsNoTracking().SingleOrDefaultAsync(t=>t.TipoEntradaId==id);
        return t is null ? null : new() { Nombre=t.Nombre,Precio=t.Precio,Capacidad=t.Capacidad,Version=protector.Protect(LocalityVersion(t)) };
    }
    public async Task<List<AdminAvailability>> AvailabilityAsync(ClaimsPrincipal user, int id)
    {
        await access.DemandAsync(user);
        var available = await VentaEventoPublica.LocalidadesDisponibles(db).Where(l=>l.EventoId==id).ToDictionaryAsync(l=>l.TipoEntradaId,l=>l.Cupos);
        var saldo = await db.Eventos.Where(e=>e.EventoId==id).Select(e=>e.TicketsDisponibles).SingleAsync();
        var rows = await db.EventoTiposEntrada.AsNoTracking().Where(l=>l.EventoId==id).Select(l=>l.TipoEntrada).OrderBy(t=>t.Nombre).ToListAsync();
        var result = new List<AdminAvailability>();
        foreach (var t in rows)
        {
            var pending = await Commitments(id,t.TipoEntradaId,"Pendiente");
            var sold = await Commitments(id,t.TipoEntradaId,"Aprobado");
            result.Add(new(t.TipoEntradaId,t.Nombre,t.Precio,t.Capacidad,pending,sold,Math.Max(0,Math.Min(saldo,available.GetValueOrDefault(t.TipoEntradaId)))));
        }
        return result;
    }

    public async Task<int> SaveEventAsync(ClaimsPrincipal user, int id, AdminEventForm form)
    {
        await access.DemandAsync(user); DemandIsolated(); ValidateEvent(form);
        return await Write(async () =>
        {
            if (id > 0) await ReservaTransaccion.BloquearEventoAsync(db,id);
            var e = id==0 ? new Evento() : await db.Eventos.SingleOrDefaultAsync(e=>e.EventoId==id) ?? throw Invalid("Evento no encontrado.");
            if (id>0)
            {
                Match(form.Version,EventVersion(e));
                if(form.CapacidadMaxima != e.CapacidadMaxima) throw Invalid("El aforo del evento existente no se modifica en esta entrega. No se permite editar inventario.");
                if (form.Estado != e.EstadoEvento && form.Estado is "Cancelado" or "Finalizado" &&
                    await db.Compras.AnyAsync(c=>c.EventoId==id && c.EstadoPago=="Pendiente"))
                    throw Invalid("Existen compras pendientes. Resuelve su estado mediante el proceso autorizado antes de finalizar o cancelar.");
                if (e.EstadoEvento is "Finalizado" or "Cancelado" && form.Estado != e.EstadoEvento)
                    throw Invalid("La reapertura de un evento finalizado o cancelado requiere revisión independiente.");
            }
            if (await db.Eventos.AnyAsync(other=>other.EventoId!=id && other.Nombre==form.Nombre && other.Fecha==form.Fecha && other.Hora==form.Hora))
                throw Invalid("Ya existe un evento con ese nombre, fecha y hora. Revisa el listado antes de repetir el envío.");
            e.Nombre=form.Nombre; e.Fecha=form.Fecha; e.Hora=form.Hora; e.Lugar=form.Lugar; e.Descripcion=form.Descripcion;
            e.ImagenUrl=form.ImagenUrl; e.EstadoEvento=form.Estado;
            if(id==0) { e.CapacidadMaxima=form.CapacidadMaxima; e.TicketsDisponibles=form.CapacidadMaxima; e.FechaCreacion=DateTime.UtcNow; db.Eventos.Add(e); }
            await db.SaveChangesAsync();
            Audit(user,"EventSaved",e.EventoId);
            return e.EventoId;
        });
    }

    public async Task<int> SaveLocalityAsync(ClaimsPrincipal user, int id, AdminLocalityForm form)
    {
        await access.DemandAsync(user); DemandIsolated(); ValidateLocality(form);
        return await Write(async () =>
        {
            var ids=await db.EventoTiposEntrada.Where(l=>l.TipoEntradaId==id).Select(l=>l.EventoId).Distinct().OrderBy(x=>x).ToListAsync();
            // El bloqueo de catálogo serializa asociaciones; los de evento coordinan con compras/reservas.
            foreach(var eventId in ids) await ReservaTransaccion.BloquearEventoAsync(db,eventId);
            var t=id==0 ? new TipoEntrada() : await db.TiposEntrada.SingleOrDefaultAsync(t=>t.TipoEntradaId==id) ?? throw Invalid("Localidad no encontrada.");
            if(id>0)
            {
                Match(form.Version,LocalityVersion(t));
                if(ids.Count>1) throw Invalid("Localidad compartida: no se permite modificar nombre, precio o capacidad desde un evento. El modelo no tiene valores por evento.");
                foreach(var eventId in ids)
                {
                    var pending=await Commitments(eventId,id,"Pendiente"); var sold=await Commitments(eventId,id,"Aprobado");
                    var tickets=await db.Tickets.LongCountAsync(x=>x.EventoId==eventId && x.TipoEntradaId==id && (x.Estado=="Activo" || x.Estado=="Usado"));
                    if(form.Capacidad < pending+Math.Max(sold,tickets)) throw Invalid("La capacidad no puede ser inferior a las reservas y entradas comprometidas.");
                    if(form.Precio!=t.Precio && pending>0) throw Invalid("Hay compras pendientes: no se cambia el precio hasta resolverlas.");
                    await CheckCapacity(eventId,id,form.Capacidad);
                }
                if (ids.Count==0 && (await db.DetalleCompras.AnyAsync(d=>d.TipoEntradaId==id) || await db.Tickets.AnyAsync(x=>x.TipoEntradaId==id)))
                    throw Invalid("Existen referencias históricas sin asociación; se requiere revisión antes de editar.");
            }
            if(await db.TiposEntrada.AnyAsync(other=>other.TipoEntradaId!=id && other.Nombre==form.Nombre))
                throw Invalid("El nombre de localidad ya existe en el catálogo global.");
            t.Nombre=form.Nombre; t.Precio=form.Precio; t.Capacidad=form.Capacidad;
            if(id==0) db.TiposEntrada.Add(t);
            await db.SaveChangesAsync(); Audit(user,"LocalitySaved",t.TipoEntradaId); return t.TipoEntradaId;
        });
    }
    public async Task AssociateAsync(ClaimsPrincipal user,int eventId,int localityId)
    {
        await access.DemandAsync(user); DemandIsolated();
        await Write(async () =>
        {
            await ReservaTransaccion.BloquearEventoAsync(db,eventId);
            if(!await db.Eventos.AnyAsync(e=>e.EventoId==eventId)) throw Invalid("Evento no encontrado.");
            var t=await db.TiposEntrada.SingleOrDefaultAsync(t=>t.TipoEntradaId==localityId) ?? throw Invalid("Selecciona una localidad existente.");
            if(await db.EventoTiposEntrada.AnyAsync(l=>l.EventoId==eventId && l.TipoEntradaId==localityId)) return 0;
            await CheckCapacity(eventId,0,t.Capacidad);
            db.EventoTiposEntrada.Add(new EventoTipoEntrada { EventoId=eventId,TipoEntradaId=localityId });
            await db.SaveChangesAsync(); Audit(user,"LocalityAssociated",eventId); return 0;
        });
    }
    private async Task CheckCapacity(int eventId,int excludeType,int proposed)
    {
        var cap=await db.Eventos.Where(e=>e.EventoId==eventId).Select(e=>e.CapacidadMaxima).SingleAsync();
        var others=await db.EventoTiposEntrada.Where(l=>l.EventoId==eventId && l.TipoEntradaId!=excludeType).SumAsync(l=>(long)l.TipoEntrada.Capacidad);
        if(others+proposed>cap) throw Invalid("La suma de capacidades de las localidades supera el aforo del evento.");
    }
    private Task<long> Commitments(int eventId,int type,string state) => db.DetalleCompras.Where(d=>d.Compra.EventoId==eventId && d.TipoEntradaId==type && d.Compra.EstadoPago==state).SumAsync(d=>(long)d.Cantidad);
    private async Task<T> Write<T>(Func<Task<T>> action)
    {
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'UGP:AdminCatalog', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @r<0 THROW 51011, 'Catalog lock unavailable', 1;");
            var result=await action(); await transaction.CommitAsync();
            logger.LogInformation("AdminAudit Catalog transaction committed");
            return result;
        });
    }
    private void DemandIsolated()
    {
        var c=new SqlConnectionStringBuilder(db.Database.GetConnectionString());
        if(!environment.IsDevelopment() || !string.Equals(c.DataSource,@"(localdb)\MSSQLLocalDB",StringComparison.OrdinalIgnoreCase) || c.InitialCatalog!="UGP_Integracion_Test" || !c.IntegratedSecurity)
            throw Invalid("Las escrituras administrativas de esta entrega están limitadas al entorno de integración autorizado.");
    }
    public static void ValidateEvent(AdminEventForm f)
    {
        f.Nombre=f.Nombre?.Trim() ?? ""; f.Lugar=f.Lugar?.Trim() ?? ""; f.Descripcion=f.Descripcion?.Trim(); f.ImagenUrl=string.IsNullOrWhiteSpace(f.ImagenUrl)?null:f.ImagenUrl.Trim();
        Validator.ValidateObject(f,new ValidationContext(f),true);
        if(!States.Contains(f.Estado) || f.Fecha.Year<2000 || f.Fecha.Year>2100) throw Invalid("Estado o fecha de evento no válido.");
        if(f.ImagenUrl is not null && MediaUrl.Image(f.ImagenUrl) is null) throw Invalid("Imagen no permitida. Usa /images/ o una imagen HTTPS del dominio editorial UGP.");
    }
    public static void ValidateLocality(AdminLocalityForm f)
    {
        f.Nombre=f.Nombre?.Trim() ?? ""; Validator.ValidateObject(f,new ValidationContext(f),true);
        if(decimal.Round(f.Precio,2)!=f.Precio) throw Invalid("El precio admite como máximo dos decimales.");
    }
    private static string Hash(object value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));
    private static string EventVersion(Evento e)=>"event:"+e.EventoId+":"+Hash(new { e.Nombre,e.Fecha,e.Hora,e.Lugar,e.Descripcion,e.ImagenUrl,e.EstadoEvento,e.CapacidadMaxima });
    private static string LocalityVersion(TipoEntrada t)=>"type:"+t.TipoEntradaId+":"+Hash(new {t.Nombre,t.Precio,t.Capacidad});
    private void Match(string token,string expected)
    {
        try { if(!string.IsNullOrEmpty(token) && token.Length<=2048 && protector.Unprotect(token)==expected) return; } catch(CryptographicException) { }
        throw Invalid("El registro cambió o el formulario no es válido. Recarga para revisar los datos antes de guardar.");
    }
    private void Audit(ClaimsPrincipal user,string action,int id)=>logger.LogInformation("AdminAudit Catalog {Action} Id={Id} Actor={Actor} (operación pendiente de commit)",action,id,user.FindFirstValue(ClaimTypes.NameIdentifier));
    private static ValidationException Invalid(string message)=>new(message);
}
