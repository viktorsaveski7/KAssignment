using Claims.Domain.Enums;
using Claims.Domain.Premium;

namespace Claims.Tests.Unit;

public sealed class StubPremiumCalculator : IPremiumCalculator
{
    private readonly decimal _premium;

    public StubPremiumCalculator(decimal premium = 1_000m) => _premium = premium;

    public DateOnly? LastStartDate { get; private set; }

    public DateOnly? LastEndDate { get; private set; }

    public CoverType? LastCoverType { get; private set; }

    public int CallCount { get; private set; }

    public decimal Compute(DateOnly startDate, DateOnly endDate, CoverType coverType)
    {
        LastStartDate = startDate;
        LastEndDate = endDate;
        LastCoverType = coverType;
        CallCount++;
        return _premium;
    }
}
