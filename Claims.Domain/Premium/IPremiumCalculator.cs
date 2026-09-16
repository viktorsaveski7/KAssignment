using Claims.Domain.Enums;

namespace Claims.Domain.Premium;

public interface IPremiumCalculator
{
    decimal Compute(DateTime startDate, DateTime endDate, CoverType coverType);
}
