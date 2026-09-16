using FluentValidation;

namespace Claims.Application.Covers.Commands.CreateCover;

public sealed class CreateCoverCommandValidator : AbstractValidator<CreateCoverCommand>
{
    private readonly TimeProvider _timeProvider;

    public CreateCoverCommandValidator(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;

        RuleFor(command => command.StartDate)
            .Must(NotBeInThePast)
            .WithMessage("StartDate cannot be in the past.");

        RuleFor(command => command.EndDate)
            .GreaterThanOrEqualTo(command => command.StartDate)
            .WithMessage("EndDate cannot be before StartDate.");

        RuleFor(command => command)
            .Must(NotExceedOneYear)
            .WithName(nameof(CreateCoverCommand.EndDate))
            .WithMessage("The total insurance period cannot exceed one year.");

        RuleFor(command => command.Type)
            .IsInEnum()
            .WithMessage("Type is not a recognised cover type.");
    }

    private bool NotBeInThePast(DateOnly startDate) =>
        startDate >= DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

    private static bool NotExceedOneYear(CreateCoverCommand command) =>
        command.EndDate <= command.StartDate.AddYears(1).AddDays(-1);
}
