namespace ImmoDigger.Domain.Common;

/// <summary>
/// Allowed values for <see cref="Entities.PropertyListing.UrbanisticStatus"/>:
/// what the listing's own text says about its planning situation. Kept as
/// string constants for the same reason as <see cref="RiskLevel"/>.
/// </summary>
public static class UrbanisticStatus
{
    /// <summary>The listing mentions an infraction or something left to regularise.</summary>
    public const string Infraction = "Infraction";

    /// <summary>The listing states there is no infraction, or that everything is in order.</summary>
    public const string Compliant = "Compliant";

    /// <summary>The listing says nothing usable either way: to be checked.</summary>
    public const string Unknown = "Unknown";
}
