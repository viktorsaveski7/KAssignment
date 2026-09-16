using Claims.Domain.Enums;
using Claims.Domain.Premium;

namespace Claims.Domain.Entities;

public class Cover
{
    private Cover()
    {
        Id = string.Empty;
    }

    private Cover(string id, DateTime startDate, DateTime endDate, CoverType type, decimal premium)
    {
        Id = id;
        StartDate = startDate;
        EndDate = endDate;
        Type = type;
        Premium = premium;
    }

    public string Id { get; private set; }

    public DateTime StartDate { get; private set; }

    public DateTime EndDate { get; private set; }

    public CoverType Type { get; private set; }

    public decimal Premium { get; private set; }

    public static Cover Create(
        DateTime startDate,
        DateTime endDate,
        CoverType type,
        IPremiumCalculator premiumCalculator)
    {
        ArgumentNullException.ThrowIfNull(premiumCalculator);

        var premium = premiumCalculator.Compute(startDate, endDate, type);
        return new Cover(Guid.NewGuid().ToString(), startDate, endDate, type, premium);
    }

    public bool CoversDate(DateTime date) => date.Date >= StartDate.Date && date.Date <= EndDate.Date;
}
