using Claims.Domain.Enums;

namespace Claims.Domain.Premium;

public interface IPremiumCalculator
{
    decimal Compute(DateOnly startDate, DateOnly endDate, CoverType coverType);
}
