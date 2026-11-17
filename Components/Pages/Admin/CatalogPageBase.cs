using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using UGP_ultimo_guerrero_en_pie_.Services.Administration;

namespace UGP_ultimo_guerrero_en_pie_.Components.Pages.Admin;

public abstract class CatalogPageBase : ComponentBase
{
    [Inject] protected AdminCatalogService Catalog { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    [Inject] protected ILogger<CatalogPageBase> Logger { get; set; } = default!;
    [CascadingParameter] public Task<AuthenticationState> AuthenticationState { get; set; } = default!;
    [CascadingParameter] public HttpContext Context { get; set; } = default!;
    protected ClaimsPrincipal User = new();
    protected string? Error;
    protected bool Ready;
    protected async Task Initialize(Func<Task> load)
    {
        User=(await AuthenticationState).User;
        Context.Response.Headers.CacheControl="no-store";
        try { await load(); Ready=true; }
        catch(Exception ex) { Fail(ex); }
    }
    protected async Task Save(Func<Task<string>> operation)
    {
        string path;
        try { path=await operation(); }
        catch(Exception ex) { Fail(ex); return; }
        Navigation.NavigateTo(path,forceLoad:true);
    }
    private void Fail(Exception ex)
    {
        Error=ex is ValidationException ? ex.Message : "No pudimos completar la operación. Revisa tu acceso o inténtalo nuevamente.";
        Logger.LogWarning("AdminCatalog operación no completada: {Type}",ex.GetType().Name);
    }
}
