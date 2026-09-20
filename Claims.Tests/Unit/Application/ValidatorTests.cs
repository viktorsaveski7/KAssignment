using Claims.Application.Claims.Commands.CreateClaim;
using Claims.Application.Covers.Commands.CreateCover;
using Claims.Application.Covers.Queries.ComputePremium;
using Claims.Domain.Enums;
using Microsoft.Extensions.Time.Testing;

namespace Claims.Tests.Unit.Application;

public class CreateClaimCommandValidatorTests
{
    private readonly CreateClaimCommandValidator _validator = new();

    private static CreateClaimCommand Command(decimal damageCost = 500m, string name = "Hull", string coverId = "c1") =>
        new(coverId, name, ClaimType.Collision, new DateOnly(2027, 2, 1), damageCost);

    [Theory]
    [InlineData(0.01)]
    [InlineData(1)]
    [InlineData(99_999)]
    [InlineData(100_000)]
    public void Accepts_damage_cost_up_to_the_limit(decimal damageCost)
    {
        Assert.True(_validator.Validate(Command(damageCost)).IsValid);
    }

    [Theory]
    [InlineData(100_000.01)]
    [InlineData(100_001)]
    [InlineData(1_000_000)]
    public void Rejects_damage_cost_above_the_limit(decimal damageCost)
    {
        var result = _validator.Validate(Command(damageCost));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("cannot exceed"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rejects_a_non_positive_damage_cost(decimal damageCost)
    {
        Assert.False(_validator.Validate(Command(damageCost)).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_a_missing_name(string name)
    {
        var result = _validator.Validate(Command(name: name));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("Name is required"));
    }

    [Fact]
    public void Rejects_a_missing_cover_id()
    {
        var result = _validator.Validate(Command(coverId: ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("CoverId is required"));
    }

    [Fact]
    public void Rejects_a_claim_type_outside_the_enum()
    {
        var command = new CreateClaimCommand("c1", "Hull", (ClaimType)99, new DateOnly(2027, 2, 1), 500m);

        Assert.False(_validator.Validate(command).IsValid);
    }
}

public class CreateCoverCommandValidatorTests
{
    private static readonly DateTimeOffset Today = new(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static CreateCoverCommandValidator ValidatorAt(DateTimeOffset now) =>
        new(new FakeTimeProvider(now));

    private static CreateCoverCommand Command(DateOnly start, DateOnly end, CoverType type = CoverType.Yacht) =>
        new(start, end, type);

    [Fact]
    public void Accepts_a_start_date_of_today()
    {
        var result = ValidatorAt(Today)
            .Validate(Command(new DateOnly(2027, 1, 1), new DateOnly(2027, 3, 1)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Accepts_a_start_date_in_the_future()
    {
        var result = ValidatorAt(Today)
            .Validate(Command(new DateOnly(2027, 5, 1), new DateOnly(2027, 8, 1)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Rejects_a_start_date_one_day_in_the_past()
    {
        var result = ValidatorAt(Today)
            .Validate(Command(new DateOnly(2026, 12, 31), new DateOnly(2027, 3, 1)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("cannot be in the past"));
    }

    [Fact]
    public void Rejects_an_end_date_before_the_start_date()
    {
        var result = ValidatorAt(Today)
            .Validate(Command(new DateOnly(2027, 6, 1), new DateOnly(2027, 5, 1)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("before StartDate"));
    }

    [Fact]
    public void Accepts_a_period_of_exactly_one_year()
    {
        var result = ValidatorAt(Today)
            .Validate(Command(new DateOnly(2027, 1, 1), new DateOnly(2027, 12, 31)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Rejects_a_period_one_day_over_a_year()
    {
        var result = ValidatorAt(Today)
            .Validate(Command(new DateOnly(2027, 1, 1), new DateOnly(2028, 1, 1)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("exceed one year"));
    }

    [Fact]
    public void Accepts_a_single_day_cover()
    {
        var result = ValidatorAt(Today)
            .Validate(Command(new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 1)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Handles_a_leap_year_period_of_one_year()
    {
        var result = ValidatorAt(new DateTimeOffset(2028, 1, 1, 0, 0, 0, TimeSpan.Zero))
            .Validate(Command(new DateOnly(2028, 1, 1), new DateOnly(2028, 12, 31)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Rejects_a_cover_type_outside_the_enum()
    {
        var result = ValidatorAt(Today)
            .Validate(Command(new DateOnly(2027, 1, 1), new DateOnly(2027, 3, 1), (CoverType)42));

        Assert.False(result.IsValid);
    }
}

public class ComputePremiumQueryValidatorTests
{
    private readonly ComputePremiumQueryValidator _validator = new();

    [Fact]
    public void Accepts_a_well_ordered_period_even_when_it_is_in_the_past()
    {
        var query = new ComputePremiumQuery(new DateOnly(2001, 1, 1), new DateOnly(2001, 3, 1), CoverType.Yacht);

        Assert.True(_validator.Validate(query).IsValid);
    }

    [Fact]
    public void Rejects_an_end_date_before_the_start_date()
    {
        var query = new ComputePremiumQuery(new DateOnly(2027, 3, 1), new DateOnly(2027, 1, 1), CoverType.Yacht);

        Assert.False(_validator.Validate(query).IsValid);
    }

    [Fact]
    public void Rejects_a_cover_type_outside_the_enum()
    {
        var query = new ComputePremiumQuery(new DateOnly(2027, 1, 1), new DateOnly(2027, 3, 1), (CoverType)42);

        Assert.False(_validator.Validate(query).IsValid);
    }
}
