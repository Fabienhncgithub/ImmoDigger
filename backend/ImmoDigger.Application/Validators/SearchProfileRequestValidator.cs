using FluentValidation;
using ImmoDigger.Application.DTOs;

namespace ImmoDigger.Application.Validators;

public class SearchProfileRequestValidator : AbstractValidator<SearchProfileRequest>
{
    public SearchProfileRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.MaximumPrice).GreaterThanOrEqualTo(0).When(x => x.MaximumPrice.HasValue);
        RuleFor(x => x.MinimumGrossYield).InclusiveBetween(0, 100).When(x => x.MinimumGrossYield.HasValue);
        RuleFor(x => x.MinimumUnitCount).GreaterThanOrEqualTo(0).When(x => x.MinimumUnitCount.HasValue);
        RuleFor(x => x.MinimumLivingArea).GreaterThanOrEqualTo(0).When(x => x.MinimumLivingArea.HasValue);
        RuleFor(x => x.MinimumOpportunityScore).InclusiveBetween(0, 100).When(x => x.MinimumOpportunityScore.HasValue);
        RuleFor(x => x.PostalCodes).NotNull();
        RuleFor(x => x.PropertyTypes).NotNull();
    }
}
