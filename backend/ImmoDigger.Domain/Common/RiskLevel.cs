namespace ImmoDigger.Domain.Common;

/// <summary>
/// Allowed values for <see cref="Entities.PropertyListing.RiskLevel"/>.
/// Kept as string constants (rather than an enum) because the value is
/// stored as free text and displayed directly, and because the risk
/// analysis service (added in a later commit) composes it from multiple
/// independent signals.
/// </summary>
public static class RiskLevel
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";
}
