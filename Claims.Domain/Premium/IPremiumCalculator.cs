using Claims.Domain.Enums;

namespace Claims.Domain.Premium;

/// <summary>Computes the premium payable for an insurance period.</summary>
public interface IPremiumCalculator
{
    decimal Compute(DateOnly startDate, DateOnly endDate, CoverType coverType);
}
