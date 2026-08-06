namespace ImmoDigger.Domain.Entities;

/// <summary>
/// A saved investment criteria profile (e.g. "Immeuble Bruxelles: max
/// 750 000 €, 3 logements minimum, rendement brut >= 5.5%, ..."). Active
/// profiles are matched against new listings to decide whether to send a
/// notification.
/// </summary>
public class SearchProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public decimal? MaximumPrice { get; set; }

    public decimal? MinimumGrossYield { get; set; }

    public int? MinimumUnitCount { get; set; }

    public decimal? MinimumLivingArea { get; set; }

    public bool RequireGarage { get; set; }

    public bool IncludePublicSales { get; set; } = true;

    public string[] PostalCodes { get; set; } = [];

    public string[] PropertyTypes { get; set; } = [];

    /// <summary>
    /// Minimum <see cref="PropertyListing.OpportunityScore"/> a listing must
    /// reach for this profile to trigger a notification. Not part of the
    /// originally specified field list, but required by the notification
    /// rules ("dépasse le score minimum configuré") - added deliberately.
    /// </summary>
    public decimal? MinimumOpportunityScore { get; set; }

    public bool IsEnabled { get; set; } = true;
}
