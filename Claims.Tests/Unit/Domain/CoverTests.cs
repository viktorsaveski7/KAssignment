using Claims.Domain.Entities;
using Claims.Domain.Enums;

namespace Claims.Tests.Unit.Domain;

public class CoverTests
{
    private static readonly DateOnly Start = new(2027, 1, 1);
    private static readonly DateOnly End = new(2027, 6, 30);

    private static StubPremiumCalculator CalculatorReturning(decimal premium) => new(premium);

    [Fact]
    public void Create_assigns_a_unique_identifier()
    {
        var calculator = CalculatorReturning(100m);

        var first = Cover.Create(Start, End, CoverType.Yacht, calculator);
        var second = Cover.Create(Start, End, CoverType.Yacht, calculator);

        Assert.False(string.IsNullOrWhiteSpace(first.Id));
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Create_stores_the_supplied_values()
    {
        var cover = Cover.Create(Start, End, CoverType.Tanker, CalculatorReturning(100m));

        Assert.Equal(Start, cover.StartDate);
        Assert.Equal(End, cover.EndDate);
        Assert.Equal(CoverType.Tanker, cover.Type);
    }

    [Fact]
    public void Create_takes_its_premium_from_the_calculator()
    {
        var cover = Cover.Create(Start, End, CoverType.Tanker, CalculatorReturning(4_242m));

        Assert.Equal(4_242m, cover.Premium);
    }

    [Fact]
    public void Create_passes_the_period_and_type_to_the_calculator()
    {
        var calculator = CalculatorReturning(1m);

        Cover.Create(Start, End, CoverType.BulkCarrier, calculator);

        Assert.Equal(1, calculator.CallCount);
        Assert.Equal(Start, calculator.LastStartDate);
        Assert.Equal(End, calculator.LastEndDate);
        Assert.Equal(CoverType.BulkCarrier, calculator.LastCoverType);
    }

    [Fact]
    public void Create_rejects_a_missing_calculator()
    {
        Assert.Throws<ArgumentNullException>(
            () => Cover.Create(Start, End, CoverType.Yacht, null!));
    }

    [Theory]
    [InlineData(2026, 12, 31, false)]
    [InlineData(2027, 1, 1, true)]
    [InlineData(2027, 3, 15, true)]
    [InlineData(2027, 6, 30, true)]
    [InlineData(2027, 7, 1, false)]
    public void CoversDate_includes_both_boundaries(int year, int month, int day, bool expected)
    {
        var cover = Cover.Create(Start, End, CoverType.Yacht, CalculatorReturning(1m));

        Assert.Equal(expected, cover.CoversDate(new DateOnly(year, month, day)));
    }

    [Fact]
    public void CoversDate_handles_a_single_day_cover()
    {
        var cover = Cover.Create(Start, Start, CoverType.Yacht, CalculatorReturning(1m));

        Assert.True(cover.CoversDate(Start));
        Assert.False(cover.CoversDate(Start.AddDays(1)));
        Assert.False(cover.CoversDate(Start.AddDays(-1)));
    }
}
