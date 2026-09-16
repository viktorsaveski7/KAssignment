using Claims.Application.Common.Behaviors;
using Claims.Domain.Common;
using FluentValidation;
using MediatR;

namespace Claims.UnitTests.Application;

public class ValidationBehaviorTests
{
    public record SampleRequest(string Name) : IRequest<Result<string>>;

    public record VoidRequest(string Name) : IRequest<Result>;

    private sealed class SampleValidator : AbstractValidator<SampleRequest>
    {
        public SampleValidator() => RuleFor(request => request.Name).NotEmpty().WithMessage("Name is required.");
    }

    private sealed class SecondSampleValidator : AbstractValidator<SampleRequest>
    {
        public SecondSampleValidator() =>
            RuleFor(request => request.Name).MinimumLength(3).WithMessage("Name is too short.");
    }

    private sealed class VoidValidator : AbstractValidator<VoidRequest>
    {
        public VoidValidator() => RuleFor(request => request.Name).NotEmpty().WithMessage("Name is required.");
    }

    [Fact]
    public async Task Calls_the_handler_when_there_are_no_validators()
    {
        var behavior = new ValidationBehavior<SampleRequest, Result<string>>([]);
        var called = false;

        var result = await behavior.Handle(
            new SampleRequest("ok"),
            () =>
            {
                called = true;
                return Task.FromResult(Result<string>.Success("handled"));
            },
            CancellationToken.None);

        Assert.True(called);
        Assert.Equal("handled", result.Value);
    }

    [Fact]
    public async Task Calls_the_handler_when_validation_passes()
    {
        var behavior = new ValidationBehavior<SampleRequest, Result<string>>([new SampleValidator()]);

        var result = await behavior.Handle(
            new SampleRequest("valid"),
            () => Task.FromResult(Result<string>.Success("handled")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("handled", result.Value);
    }

    [Fact]
    public async Task Short_circuits_the_handler_when_validation_fails()
    {
        var behavior = new ValidationBehavior<SampleRequest, Result<string>>([new SampleValidator()]);
        var called = false;

        var result = await behavior.Handle(
            new SampleRequest(""),
            () =>
            {
                called = true;
                return Task.FromResult(Result<string>.Success("handled"));
            },
            CancellationToken.None);

        Assert.False(called);
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Equal("Name is required.", result.Errors.Single());
    }

    [Fact]
    public async Task Collects_failures_from_every_validator()
    {
        var behavior = new ValidationBehavior<SampleRequest, Result<string>>(
            [new SampleValidator(), new SecondSampleValidator()]);

        var result = await behavior.Handle(
            new SampleRequest(""),
            () => Task.FromResult(Result<string>.Success("handled")),
            CancellationToken.None);

        Assert.Equal(2, result.Errors.Count);
        Assert.Contains("Name is required.", result.Errors);
        Assert.Contains("Name is too short.", result.Errors);
    }

    [Fact]
    public async Task Deduplicates_identical_messages()
    {
        var behavior = new ValidationBehavior<SampleRequest, Result<string>>(
            [new SampleValidator(), new SampleValidator()]);

        var result = await behavior.Handle(
            new SampleRequest(""),
            () => Task.FromResult(Result<string>.Success("handled")),
            CancellationToken.None);

        Assert.Single(result.Errors);
    }

    public class ResultWithoutAnInvalidFactory : Result
    {
        public ResultWithoutAnInvalidFactory() : base(ResultStatus.Success, Array.Empty<string>())
        {
        }
    }

    [Fact]
    public async Task Fails_loudly_when_the_response_type_declares_no_Invalid_factory()
    {
        var behavior = new ValidationBehavior<SampleRequest, ResultWithoutAnInvalidFactory>(
            [new SampleValidator()]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(
            new SampleRequest(""),
            () => Task.FromResult(new ResultWithoutAnInvalidFactory()),
            CancellationToken.None));

        Assert.Contains(nameof(ResultWithoutAnInvalidFactory), exception.Message);
    }

    [Fact]
    public async Task Produces_a_non_generic_failed_result_for_a_void_request()
    {
        var behavior = new ValidationBehavior<VoidRequest, Result>([new VoidValidator()]);

        var result = await behavior.Handle(
            new VoidRequest(""),
            () => Task.FromResult(Result.Success()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Equal("Name is required.", result.Errors.Single());
    }
}
