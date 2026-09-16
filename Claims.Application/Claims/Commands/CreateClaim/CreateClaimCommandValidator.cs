using FluentValidation;

namespace Claims.Application.Claims.Commands.CreateClaim;

public sealed class CreateClaimCommandValidator : AbstractValidator<CreateClaimCommand>
{
    public const decimal MaximumDamageCost = 100_000m;

    public CreateClaimCommandValidator()
    {
        RuleFor(command => command.CoverId)
            .NotEmpty()
            .WithMessage("CoverId is required.");

        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage("Name is required.");

        RuleFor(command => command.Type)
            .IsInEnum()
            .WithMessage("Type is not a recognised claim type.");

        RuleFor(command => command.DamageCost)
            .GreaterThan(0m)
            .WithMessage("DamageCost must be greater than zero.")
            .LessThanOrEqualTo(MaximumDamageCost)
            .WithMessage($"DamageCost cannot exceed {MaximumDamageCost:N0}.");
    }
}
