using FluentValidation;

namespace Claims.Application.Covers.Queries.ComputePremium;

public sealed class ComputePremiumQueryValidator : AbstractValidator<ComputePremiumQuery>
{
    public ComputePremiumQueryValidator()
    {
        RuleFor(query => query.EndDate)
            .GreaterThanOrEqualTo(query => query.StartDate)
            .WithMessage("EndDate cannot be before StartDate.");

        RuleFor(query => query.CoverType)
            .IsInEnum()
            .WithMessage("CoverType is not a recognised cover type.");
    }
}
