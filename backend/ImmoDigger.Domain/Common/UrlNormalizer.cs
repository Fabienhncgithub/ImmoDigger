namespace ImmoDigger.Domain.Common;

/// <summary>
/// Normalizes a listing URL to a stable identity key: lowercase host
/// without a leading "www.", lowercase path without a trailing slash,
/// query string and fragment stripped. Used for deduplication level 2
/// (exact normalized URL match).
/// </summary>
public static class UrlNormalizer
{
    public static string Normalize(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        var trimmed = url.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return trimmed.ToLowerInvariant();
        }

        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal))
        {
            host = host[4..];
        }

        var path = uri.AbsolutePath.TrimEnd('/').ToLowerInvariant();

        return $"{host}{path}";
    }
}
