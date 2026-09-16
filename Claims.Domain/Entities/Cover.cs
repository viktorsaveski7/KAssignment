using Claims.Domain.Enums;
using Claims.Domain.Premium;

namespace Claims.Domain.Entities;

public class Cover
{
    private Cover()
    {
        Id = string.Empty;
    }

    private Cover(string id, DateOnly startDate, DateOnly endDate, CoverType type, decimal premium)
    {
        Id = id;
        StartDate = startDate;
        EndDate = endDate;
        Type = type;
        Premium = premium;
    }

    public string Id { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public CoverType Type { get; private set; }

    public decimal Premium { get; private set; }

    public static Cover Create(
        DateOnly startDate,
        DateOnly endDate,
        CoverType type,
        IPremiumCalculator premiumCalculator)
    {
        ArgumentNullException.ThrowIfNull(premiumCalculator);

        var premium = premiumCalculator.Compute(startDate, endDate, type);
        return new Cover(Guid.NewGuid().ToString(), startDate, endDate, type, premium);
    }

    public bool CoversDate(DateOnly date) => date >= StartDate && date <= EndDate;
}
