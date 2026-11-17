using System.Text.RegularExpressions;

namespace UGP_ultimo_guerrero_en_pie_.Components.Multimedia;

// Lista explícita de medios editoriales; nunca admite HTML ni URLs de iframe arbitrarias.
public static class MediaUrl
{
    public static string? Image(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 2048 || value.Any(char.IsControl)
            || value.Contains('\\') || value.Contains('%') || value.Contains("..")) return null;
        string path;
        if (value.StartsWith("/images/", StringComparison.Ordinal)) path = value;
        else
        {
            if (!SafeHttps(value, out var uri) || uri is null
                || uri.Host is not ("ugpmma.com" or "www.ugpmma.com" or "ugpmma.com.co" or "www.ugpmma.com.co")) return null;
            path = uri.AbsolutePath;
            if (uri.Query.Length != 0 || uri.Fragment.Length != 0) return null;
        }
        return Regex.IsMatch(path, @"\.(jpg|jpeg|png|webp|avif|gif)$", RegexOptions.IgnoreCase) ? value : null;
    }

    public static string? YouTube(string? value)
    {
        if (!SafeHttps(value, out var uri) || uri is null) return null;
        string? id = null;
        if (uri.Host == "youtu.be") id = uri.AbsolutePath.TrimStart('/');
        else if (uri.Host is "youtube.com" or "www.youtube.com" or "www.youtube-nocookie.com")
        {
            var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && parts[0] is "embed" or "shorts") id = parts[1];
            else if (uri.AbsolutePath == "/watch")
            {
                var values = uri.Query.TrimStart('?').Split('&').Where(p => p.StartsWith("v=", StringComparison.Ordinal)).ToArray();
                if (values.Length == 1) id = values[0][2..];
            }
        }
        return id is not null && Regex.IsMatch(id, @"\A[A-Za-z0-9_-]{11}\z")
            ? "https://www.youtube-nocookie.com/embed/" + id : null;
    }

    private static bool SafeHttps(string? value, out Uri? uri)
    {
        uri = null;
        return value is not null && value.Length <= 2048 && !value.Any(char.IsControl)
            && !value.Contains('\\') && Uri.TryCreate(value, UriKind.Absolute, out uri)
            && uri.Scheme == "https" && uri.IsDefaultPort && uri.UserInfo.Length == 0;
    }
}

public sealed record GalleryMedia(string Url, string Title, bool IsVideo);
