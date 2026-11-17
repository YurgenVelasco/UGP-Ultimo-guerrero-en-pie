using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UGP_ultimo_guerrero_en_pie_.Components.Multimedia;

internal static class GalleryChecks
{
    public static async Task Run()
    {
        var count = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception("FALLA: " + label); count++; Console.WriteLine("PASA: " + label); }
        foreach (var url in new[] { "javascript:alert(1)", "data:image/png;base64,x", "//evil.test/a.jpg", "https://ugpmma.com.evil.test/a.jpg", "https://ugpmma.com@evil.test/a.jpg", "/images/../private.jpg", "/images/%2e%2e/a.jpg", "https://ugpmma.com:8443/a.jpg", "http://ugpmma.com/a.jpg", "/images/a.svg", "/images/a.jpg?x=1" })
            Check(MediaUrl.Image(url) is null, "rechazo imagen insegura " + count);
        Check(MediaUrl.Image("/images/ugp/portada-movil.jpg") is not null, "imagen local permitida");
        Check(MediaUrl.Image("https://ugpmma.com/wp-content/uploads/photo.webp") is not null, "imagen editorial HTTPS permitida");
        foreach (var url in new[] { "https://youtube.com.evil.test/embed/abcdefghijk", "javascript:alert(1)", "https://youtube.com/watch?v=abcdefghijk&v=anotherabcd", "https://youtu.be/abc", "https://evil.test/embed/abcdefghijk", "http://youtube.com/watch?v=abcdefghijk" })
            Check(MediaUrl.YouTube(url) is null, "rechazo video inseguro " + count);
        Check(MediaUrl.YouTube("https://www.youtube.com/watch?v=abcdefghijk&autoplay=1") == "https://www.youtube-nocookie.com/embed/abcdefghijk", "URL canónica sin autoplay ni parámetros externos");
        Check(MediaUrl.YouTube("https://youtu.be/abcdefghijk") == "https://www.youtube-nocookie.com/embed/abcdefghijk", "enlace corto reconocido");
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var rendered = await renderer.RenderComponentAsync<GalleryCarousel>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["Items"] = new List<GalleryMedia> { new("/images/a.jpg", "<script>alert(1)</script>", false), new("https://www.youtube-nocookie.com/embed/abcdefghijk", "Video", true) }
            }));
            return rendered.ToHtmlString();
        });
        Check(!html.Contains("<script>") && html.Contains("&lt;script&gt;"), "texto de base escapado por Razor");
        Check(html.Contains("aria-roledescription=\"carrusel\"") && html.Contains("aria-current=\"true\""), "semántica e indicador de carrusel");
        var videoHtml = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<MediaCard>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            ["Item"] = new GalleryMedia("https://www.youtube-nocookie.com/embed/abcdefghijk", "Video", true)
        }))).ToHtmlString());
        Check(!videoHtml.Contains("<iframe") && videoHtml.Contains("Cargar video"), "YouTube no se conecta antes del consentimiento");
        Console.WriteLine($"{count} comprobaciones de galería aprobadas. Sin SQL ni HTTP externo; recursos exclusivamente de fixture.");
    }
}
