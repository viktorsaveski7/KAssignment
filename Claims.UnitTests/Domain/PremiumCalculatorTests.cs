using Claims.Domain.Enums;
using Claims.Domain.Premium;

namespace Claims.UnitTests.Domain;

public class PremiumCalculatorTests
{
    private static readonly DateOnly Start = new(2030, 1, 1);
    private readonly PremiumCalculator _calculator = new();

    private decimal PremiumFor(int insuredDays, CoverType coverType) =>
        _calculator.Compute(Start, Start.AddDays(insuredDays - 1), coverType);

    private const decimal BaseDayRate = 1250m;

    private static decimal DayRate(CoverType coverType) => coverType switch
    {
        CoverType.Yacht => BaseDayRate * 1.1m,
        CoverType.PassengerShip => BaseDayRate * 1.2m,
        CoverType.Tanker => BaseDayRate * 1.5m,
        _ => BaseDayRate * 1.3m
    };

    [Theory]
    [InlineData(CoverType.Yacht, 1375)]
    [InlineData(CoverType.PassengerShip, 1500)]
    [InlineData(CoverType.Tanker, 1875)]
    [InlineData(CoverType.ContainerShip, 1625)]
    [InlineData(CoverType.BulkCarrier, 1625)]
    public void A_single_day_costs_one_full_day_rate(CoverType coverType, decimal expected)
    {
        Assert.Equal(expected, PremiumFor(1, coverType));
    }

    [Theory]
    [InlineData(CoverType.Yacht, 1.1)]
    [InlineData(CoverType.PassengerShip, 1.2)]
    [InlineData(CoverType.Tanker, 1.5)]
    [InlineData(CoverType.ContainerShip, 1.3)]
    [InlineData(CoverType.BulkCarrier, 1.3)]
    public void The_type_multiplier_is_applied_to_the_base_day_rate(CoverType coverType, decimal multiplier)
    {
        Assert.Equal(BaseDayRate * multiplier, PremiumFor(1, coverType));
    }

    [Theory]
    [InlineData(CoverType.Yacht)]
    [InlineData(CoverType.Tanker)]
    [InlineData(CoverType.ContainerShip)]
    public void The_first_thirty_days_are_charged_at_the_full_day_rate(CoverType coverType)
    {
        Assert.Equal(30 * DayRate(coverType), PremiumFor(30, coverType));
    }

    [Fact]
    public void Day_thirty_one_starts_the_discounted_band_for_a_yacht()
    {
        var dayRate = DayRate(CoverType.Yacht);

        var thirtyDays = PremiumFor(30, CoverType.Yacht);
        var thirtyOneDays = PremiumFor(31, CoverType.Yacht);

        Assert.Equal(dayRate * 0.95m, thirtyOneDays - thirtyDays);
    }

    [Fact]
    public void Day_thirty_one_starts_the_discounted_band_for_other_types()
    {
        var dayRate = DayRate(CoverType.Tanker);

        var thirtyDays = PremiumFor(30, CoverType.Tanker);
        var thirtyOneDays = PremiumFor(31, CoverType.Tanker);

        Assert.Equal(dayRate * 0.98m, thirtyOneDays - thirtyDays);
    }

    [Fact]
    public void The_discounted_band_runs_for_one_hundred_and_fifty_days()
    {
        var dayRate = DayRate(CoverType.Yacht);

        Assert.Equal(
            (30 * dayRate) + (150 * dayRate * 0.95m),
            PremiumFor(180, CoverType.Yacht));
    }

    [Fact]
    public void Day_one_hundred_and_eighty_one_starts_the_further_discounted_band_for_a_yacht()
    {
        var dayRate = DayRate(CoverType.Yacht);

        var oneEighty = PremiumFor(180, CoverType.Yacht);
        var oneEightyOne = PremiumFor(181, CoverType.Yacht);

        Assert.Equal(dayRate * 0.92m, oneEightyOne - oneEighty);
    }

    [Fact]
    public void Day_one_hundred_and_eighty_one_starts_the_further_discounted_band_for_other_types()
    {
        var dayRate = DayRate(CoverType.Tanker);

        var oneEighty = PremiumFor(180, CoverType.Tanker);
        var oneEightyOne = PremiumFor(181, CoverType.Tanker);

        Assert.Equal(dayRate * 0.97m, oneEightyOne - oneEighty);
    }

    [Theory]
    [InlineData(CoverType.Yacht, 471212.5)]
    [InlineData(CoverType.PassengerShip, 534675)]
    [InlineData(CoverType.Tanker, 668343.75)]
    [InlineData(CoverType.ContainerShip, 579231.25)]
    [InlineData(CoverType.BulkCarrier, 579231.25)]
    public void A_full_year_is_priced_across_all_three_bands(CoverType coverType, decimal expected)
    {
        Assert.Equal(expected, PremiumFor(365, coverType));
    }

    [Fact]
    public void The_period_is_inclusive_of_both_end_dates()
    {
        var premium = _calculator.Compute(
            new DateOnly(2030, 1, 1),
            new DateOnly(2030, 1, 10),
            CoverType.Yacht);

        Assert.Equal(10 * DayRate(CoverType.Yacht), premium);
    }

    [Fact]
    public void Days_beyond_a_year_keep_being_charged()
    {
        var oneYear = PremiumFor(365, CoverType.Yacht);
        var oneYearAndOneDay = PremiumFor(366, CoverType.Yacht);

        Assert.Equal(DayRate(CoverType.Yacht) * 0.92m, oneYearAndOneDay - oneYear);
    }

    [Theory]
    [InlineData(CoverType.Yacht)]
    [InlineData(CoverType.Tanker)]
    public void Each_extra_day_increases_the_premium(CoverType coverType)
    {
        var previous = 0m;

        for (var days = 1; days <= 400; days++)
        {
            var premium = PremiumFor(days, coverType);
            Assert.True(premium > previous, $"day {days} did not increase the premium");
            previous = premium;
        }
    }

    [Theory]
    [InlineData(CoverType.Yacht)]
    [InlineData(CoverType.Tanker)]
    public void Later_days_never_cost_more_than_earlier_days(CoverType coverType)
    {
        var previousDayCost = decimal.MaxValue;

        for (var days = 2; days <= 400; days++)
        {
            var dayCost = PremiumFor(days, coverType) - PremiumFor(days - 1, coverType);
            Assert.True(dayCost <= previousDayCost, $"day {days} cost more than the day before it");
            previousDayCost = dayCost;
        }
    }

    [Fact]
    public void A_reversed_period_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => _calculator.Compute(
            new DateOnly(2030, 6, 30),
            new DateOnly(2030, 1, 1),
            CoverType.Yacht));
    }
}
