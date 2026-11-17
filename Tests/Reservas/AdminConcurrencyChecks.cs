using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using UGP_ultimo_guerrero_en_pie_.Data;
using UGP_ultimo_guerrero_en_pie_.Models.ViewModels;
using UGP_ultimo_guerrero_en_pie_.Services.Administration;

internal static class AdminConcurrencyChecks
{
    public static async Task Run()
    {
        if(!await AdminIntegrationPreflight.Run()) throw new Exception("Precondiciones incompletas");
        var evidence=new List<string>();
        void Check(bool ok,string label){var line=(ok?"PASA: ":"FALLA: ")+label;evidence.Add(line);Console.WriteLine(line);if(!ok)throw new Exception(label);}
        var protector=new EphemeralDataProtectionProvider();
        ApplicationDbContext Db(IInterceptor? fault=null){var b=new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(AdminIntegrationPreflight.Connection,o=>o.EnableRetryOnFailure(3,TimeSpan.FromSeconds(1),[51011,51013]));if(fault!=null)b.AddInterceptors(fault);return new(b.Options);}
        AdminCatalogService Service(ApplicationDbContext db)=>new(db,new AdminOperations(new Allowed(),NullLogger<AdminOperations>.Instance),protector,new FixtureHost(),NullLogger<AdminCatalogService>.Instance);
        ClaimsPrincipal User(string id)=>new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,id)],"isolated-fixture"));
        var u=User("admin-A"); var v=User("admin-B");
        var marker="QA631-"+Guid.NewGuid().ToString("N")[..10];
        await using var read=Db();var service=Service(read);
        var id=await service.SaveEventAsync(u,0,new(){Nombre=marker,Lugar="SQL concurrencia",Fecha=new DateOnly(2030,3,1),CapacidadMaxima=20,Estado="Programado"});
        async Task Race(bool states)
        {
            var a=(await service.EventAsync(u,id))!.Value.Form;var b=(await service.EventAsync(v,id))!.Value.Form;
            if(states){a.Estado="Activo";b.Estado="Próximo";}else{a.Lugar="Editor A";b.Lugar="Editor B";}
            var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            async Task<bool> Edit(ClaimsPrincipal actor,AdminEventForm f){await using var db=Db();await gate.Task;try{await Service(db).SaveEventAsync(actor,id,f);return true;}catch(ValidationException){return false;}}
            var t1=Edit(u,a);var t2=Edit(v,b);gate.SetResult();var results=await Task.WhenAll(t1,t2);
            var current=(await service.EventAsync(u,id))!.Value.Event;
            Check(results.Count(x=>x)==1,states?"Estados simultáneos: un commit y un conflicto explícito":"Edición simultánea: un commit y un conflicto explícito");
            Check(states?current.EstadoEvento==(results[0]?a.Estado:b.Estado):current.Lugar==(results[0]?a.Lugar:b.Lugar),"SQL conserva exactamente la actualización ganadora");
        }
        await Race(false);await Race(true);
        var type=await service.SaveLocalityAsync(u,0,new(){Nombre=marker,Precio=10000,Capacidad=10});
        var start=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task Associate(ClaimsPrincipal actor){await using var db=Db();await start.Task;await Service(db).AssociateAsync(actor,id,type);}
        var one=Associate(u);var two=Associate(v);start.SetResult();await Task.WhenAll(one,two);
        Check(await read.EventoTiposEntrada.CountAsync(x=>x.EventoId==id && x.TipoEntradaId==type)==1,"Asociaciones simultáneas: una fila única");
        var fault=new TransientAfterSave();
        await using(var db=Db(fault))
        {
            var form=(await service.EventAsync(u,id))!.Value.Form;form.Lugar="Reintento confirmado";
            await Service(db).SaveEventAsync(u,id,form);
        }
        Check(fault.Calls==2 && (await service.EventAsync(u,id))!.Value.Event.Lugar=="Reintento confirmado","EF reintenta fallo Timeout inyectado después de SaveChanges y antes de COMMIT");
        await using(var cut=new SqlConnection(AdminIntegrationPreflight.Connection+";Pooling=false"))
        {
            await cut.OpenAsync();await using var tx=await cut.BeginTransactionAsync();await using var cmd=cut.CreateCommand();cmd.Transaction=(SqlTransaction)tx;
            cmd.CommandText="UPDATE dbo.Eventos SET lugar=@place WHERE evento_id=@id";cmd.Parameters.AddWithValue("@place","NO DEBE PERSISTIR");cmd.Parameters.AddWithValue("@id",id);await cmd.ExecuteNonQueryAsync();
            cmd.CommandText="WAITFOR DELAY '00:00:10'; SELECT 1";var running=cmd.ExecuteScalarAsync();await Task.Delay(300);await cut.CloseAsync();
            var interrupted=false;try{await running;}catch(Exception ex)when(ex is SqlException or InvalidOperationException){interrupted=true;}
            Check(interrupted,"Desconexión real de sesión propia durante comando SQL");
        }
        Check((await service.EventAsync(u,id))!.Value.Event.Lugar=="Reintento confirmado","SQL revierte cambio no confirmado tras desconexión");
        var recovered=(await service.EventAsync(u,id))!.Value.Form;recovered.Lugar="Recuperado";await service.SaveEventAsync(u,id,recovered);
        Check((await service.EventAsync(u,id))!.Value.Event.Lugar=="Recuperado","Nueva operación administrativa funciona tras desconexión");
        Check((await service.EventAsync(u,id))!.Value.Event.TicketsDisponibles==20 && !await read.Compras.AnyAsync(x=>x.EventoId==id) && !await read.Tickets.AnyAsync(x=>x.EventoId==id),"Inventario intacto, sin compras ni tickets");
        evidence.Add($"Fixture SQL: EventoId={id}, TipoEntradaId={type}. Autorización de servicio sustituida sólo en pruebas; no son dos logins MFA.");
        Directory.CreateDirectory("artifacts");await File.WriteAllLinesAsync("artifacts/admin631-concurrency.txt",evidence);
    }
    private sealed class Allowed:IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal u,object? r,string p)=>Task.FromResult(p==AdminSettings.Policy?AuthorizationResult.Success():AuthorizationResult.Failed());
        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal u,object? r,IEnumerable<IAuthorizationRequirement> q)=>Task.FromResult(AuthorizationResult.Success());
    }
    private sealed class TransientAfterSave:SaveChangesInterceptor
    {
        public int Calls;
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData data,int result,CancellationToken token=default)
        {if(++Calls==1)throw new TimeoutException("Fallo sintético transitorio antes de COMMIT");return ValueTask.FromResult(result);}
    }
}
