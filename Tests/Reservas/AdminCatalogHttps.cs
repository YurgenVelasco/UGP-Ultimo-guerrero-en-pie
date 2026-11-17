using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using UGP_ultimo_guerrero_en_pie_.Data;

internal static class AdminCatalogHttps
{
    public static async Task Run(HttpClient client, Action<bool,string> check)
    {
        await using var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(AdminIntegrationPreflight.Connection).Options);
        using var anonymous=new HttpClient(new HttpClientHandler {AllowAutoRedirect=false}){BaseAddress=client.BaseAddress};
        var loginHtml=await anonymous.GetStringAsync("/acceso-ugp");
        var anonymousCsrf=WebUtility.HtmlDecode(Regex.Match(loginHtml,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        foreach(var path in new[]{"/administracion/eventos","/administracion/localidades"})
        {
            check((await anonymous.GetAsync(path)).StatusCode==HttpStatusCode.Unauthorized,"Anónimo GET rechazado: "+path);
            var post=await anonymous.PostAsync(path,new FormUrlEncodedContent(new Dictionary<string,string>{{"__RequestVerificationToken",anonymousCsrf},{"_handler","localidad"}}));
            check(post.StatusCode==HttpStatusCode.Unauthorized,"Anónimo POST con CSRF válido HTTP "+(int)post.StatusCode+": "+path);
        }
        foreach(var path in new[]{"/administracion/eventos","/administracion/eventos/nuevo","/administracion/localidades"})
            check((await client.GetAsync(path)).StatusCode==HttpStatusCode.OK,"Catálogo autenticado HTTPS 200: "+path);
        check((await client.PostAsync("/administracion/eventos/nuevo",new FormUrlEncodedContent(new Dictionary<string,string>{{"_handler","evento"}}))).StatusCode==HttpStatusCode.BadRequest,"Formulario evento sin CSRF rechazado HTTP 400");
        var html=await client.GetStringAsync("/administracion/eventos/nuevo");
        var fields=new Dictionary<string,string>();
        foreach(Match input in Regex.Matches(html,"<input\\b[^>]*>",RegexOptions.IgnoreCase))
        {
            var name=Regex.Match(input.Value,"name=\"([^\"]+)\"").Groups[1].Value;
            var value=Regex.Match(input.Value,"value=\"([^\"]*)\"").Groups[1].Value;
            if(name.Length>0) fields[WebUtility.HtmlDecode(name)]=WebUtility.HtmlDecode(value);
        }
        var marker="QA63-HTTP-"+Guid.NewGuid().ToString("N")[..8];
        fields["Form.Nombre"]=marker;fields["Form.Lugar"]="Laboratorio HTTPS sintético";
        fields["Form.Fecha"]="2030-02-01";fields["Form.Hora"]="18:00:00";
        fields["Form.CapacidadMaxima"]="20";fields["Form.Estado"]="Programado";
        var response=await client.PostAsync("/administracion/eventos/nuevo",new FormUrlEncodedContent(fields));
        var location=response.Headers.Location?.OriginalString;
        var success=response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.SeeOther && location is not null && Regex.IsMatch(location,@"/administracion/eventos/\d+$");
        check(success,"Creación de evento mediante formulario Blazor HTTPS y redirección");
        if(!success) return;
        var detail=await client.GetStringAsync(location);
        check(detail.Contains(marker),"Detalle administrativo conserva evento creado por HTTP");
        var id=Regex.Match(location!,@"\d+$").Value;
        var publicResponse=await client.GetAsync("/eventos/"+id);
        check(publicResponse.StatusCode==HttpStatusCode.OK && (await publicResponse.Content.ReadAsStringAsync()).Contains(marker),"Detalle público refleja creación administrativa");
        var eventId=int.Parse(id);
        var edit=Form(detail,"evento");edit["Form.Nombre"]=marker+" editado";edit["Form.Lugar"]="Laboratorio editado HTTPS";edit["Form.Estado"]="Programado";
        var edited=await client.PostAsync(location,new FormUrlEncodedContent(edit));
        check(edited.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.SeeOther,"Edición por POST Blazor redirige");
        check(await db.Eventos.AnyAsync(x=>x.EventoId==eventId && x.Nombre==marker+" editado" && x.Lugar=="Laboratorio editado HTTPS"),"SQL conserva edición HTTPS");
        check((await client.GetStringAsync("/eventos/"+id)).Contains(marker+" editado"),"Detalle público refleja edición HTTPS");
        var invalid=Form(await client.GetStringAsync(location),"evento");invalid["Form.Nombre"]="   ";invalid["Form.Estado"]="Programado";
        var rejected=await client.PostAsync(location,new FormUrlEncodedContent(invalid));
        check(rejected.StatusCode==HttpStatusCode.OK && await db.Eventos.AnyAsync(x=>x.EventoId==eventId && x.Nombre==marker+" editado"),"Nombre vacío rechazado sin cambiar SQL");
        var typeForm=Form(await client.GetStringAsync("/administracion/localidades"),"localidad");
        typeForm["Form.Nombre"]=marker;typeForm["Form.Precio"]="10000";typeForm["Form.Capacidad"]="10";
        var typeResponse=await client.PostAsync("/administracion/localidades",new FormUrlEncodedContent(typeForm));
        check(typeResponse.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.SeeOther,"Alta de localidad por HTTPS");
        var type=await db.TiposEntrada.AsNoTracking().SingleAsync(x=>x.Nombre==marker);
        var association=Form(await client.GetStringAsync(location),"asociacion");association["Association.TipoEntradaId"]=type.TipoEntradaId.ToString();
        var associated=await client.PostAsync(location,new FormUrlEncodedContent(association));
        check(associated.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.SeeOther && await db.EventoTiposEntrada.AnyAsync(x=>x.EventoId==eventId && x.TipoEntradaId==type.TipoEntradaId),"Asociación HTTPS persistida en SQL");
        association=Form(await client.GetStringAsync(location),"asociacion");association["Association.TipoEntradaId"]=type.TipoEntradaId.ToString();
        await client.PostAsync(location,new FormUrlEncodedContent(association));
        check(await db.EventoTiposEntrada.CountAsync(x=>x.EventoId==eventId && x.TipoEntradaId==type.TipoEntradaId)==1,"Reenvío HTTPS no duplica asociación");
        check(await db.Eventos.Where(x=>x.EventoId==eventId).Select(x=>x.TicketsDisponibles).SingleAsync()==20 && !await db.Tickets.AnyAsync(x=>x.EventoId==eventId),"HTTPS no descuenta inventario ni emite tickets");
        Console.WriteLine($"Fixture HTTPS: evento {eventId}, localidad {type.TipoEntradaId}");
    }
    private static Dictionary<string,string> Form(string html,string handler)
    {
        var form=Regex.Matches(html,"<form\\b[^>]*>.*?</form>",RegexOptions.Singleline|RegexOptions.IgnoreCase).Cast<Match>().Single(x=>x.Value.Contains("value=\""+handler+"\""));
        var result=new Dictionary<string,string>();
        foreach(Match input in Regex.Matches(form.Value,"<input\\b[^>]*>",RegexOptions.IgnoreCase))
        {var name=Regex.Match(input.Value,"name=\"([^\"]+)\"").Groups[1].Value;var value=Regex.Match(input.Value,"value=\"([^\"]*)\"").Groups[1].Value;if(name.Length>0)result[WebUtility.HtmlDecode(name)]=WebUtility.HtmlDecode(value);}
        return result;
    }
}
