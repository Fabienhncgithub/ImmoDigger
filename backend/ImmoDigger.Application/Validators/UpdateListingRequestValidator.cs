using FluentValidation;
using ImmoDigger.Application.DTOs;

namespace ImmoDigger.Application.Validators;

public class UpdateListingRequestValidator : AbstractValidator<UpdateListingRequest>
{
    public UpdateListingRequestValidator()
    {
        RuleFor(x => x.PersonalNotes).MaximumLength(5000);
        RuleFor(x => x.EstimatedMonthlyRentPerUnit).GreaterThanOrEqualTo(0)
            .When(x => x.EstimatedMonthlyRentPerUnit.HasValue);
        RuleFor(x => x.EstimatedAcquisitionCosts).GreaterThanOrEqualTo(0)
            .When(x => x.EstimatedAcquisitionCosts.HasValue);
        RuleFor(x => x.EstimatedRenovationBudget).GreaterThanOrEqualTo(0)
            .When(x => x.EstimatedRenovationBudget.HasValue);
    }
}
