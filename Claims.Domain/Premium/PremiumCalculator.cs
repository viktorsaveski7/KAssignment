using Claims.Domain.Enums;

namespace Claims.Domain.Premium;

/// <summary>
/// Prices a cover from its type and length: the first 30 days at the full day rate, the next 150 at a
/// discount, and the remainder at a further discount.
/// </summary>
public sealed class PremiumCalculator : IPremiumCalculator
{
    private const decimal BaseDayRate = 1250m;

    private const int FullRateDays = 30;
    private const int DiscountedDays = 150;

    public decimal Compute(DateOnly startDate, DateOnly endDate, CoverType coverType)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException(
                $"The insurance period ends ({endDate:yyyy-MM-dd}) before it starts ({startDate:yyyy-MM-dd}).",
                nameof(endDate));
        }

        var insuredDays = endDate.DayNumber - startDate.DayNumber + 1;
        var dayRate = BaseDayRate * TypeMultiplier(coverType);

        var atFullRate = Math.Min(insuredDays, FullRateDays);
        var atDiscount = Math.Clamp(insuredDays - FullRateDays, 0, DiscountedDays);
        var atFurtherDiscount = Math.Max(insuredDays - FullRateDays - DiscountedDays, 0);

        return (atFullRate * dayRate)
             + (atDiscount * dayRate * (1m - Discount(coverType)))
             + (atFurtherDiscount * dayRate * (1m - FurtherDiscount(coverType)));
    }

    private static decimal TypeMultiplier(CoverType coverType) => coverType switch
    {
        CoverType.Yacht => 1.1m,
        CoverType.PassengerShip => 1.2m,
        CoverType.Tanker => 1.5m,
        _ => 1.3m
    };

    private static decimal Discount(CoverType coverType) =>
        coverType == CoverType.Yacht ? 0.05m : 0.02m;

    // The specification calls the third band "discounted by an additional 3% / 1%", so the two
    // discounts are cumulative: 8% for a yacht, 3% for everything else.
    private static decimal FurtherDiscount(CoverType coverType) =>
        Discount(coverType) + AdditionalDiscount(coverType);

    private static decimal AdditionalDiscount(CoverType coverType) =>
        coverType == CoverType.Yacht ? 0.03m : 0.01m;
}
