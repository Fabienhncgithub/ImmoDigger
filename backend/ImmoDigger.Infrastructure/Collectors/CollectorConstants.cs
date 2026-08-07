namespace ImmoDigger.Infrastructure.Collectors;

/// <summary>Shared conventions real (non-placeholder) collectors follow.</summary>
public static class CollectorConstants
{
    /// <summary>
    /// Identifies the app clearly to every source it talks to, per the
    /// project's collection rules - no personal contact details (the app
    /// is for personal use; this string is what gets logged on someone
    /// else's server, so it stays generic).
    /// </summary>
    public const string UserAgent = "ImmoDigger/1.0 (personal, non-commercial real estate monitoring tool)";

    /// <summary>
    /// ImmoDigger's stated scope is Brussels and nearby communes. Belgian
    /// postal codes 10xx-19xx cover Brussels-Capital plus all of Flemish
    /// and Walloon Brabant, immediately surrounding it - a simple, honest
    /// proxy for "nearby" without hand-maintaining a commune allow-list
    /// inside every collector.
    /// </summary>
    public static bool IsInTargetArea(string? postalCode) =>
        !string.IsNullOrWhiteSpace(postalCode) && postalCode.Length == 4 && postalCode[0] == '1';
}
