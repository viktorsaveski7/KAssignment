using Claims.Domain.Common;
using Claims.Domain.Premium;
using MediatR;

namespace Claims.Application.Covers.Queries.ComputePremium;

public sealed class ComputePremiumQueryHandler : IRequestHandler<ComputePremiumQuery, Result<decimal>>
{
    private readonly IPremiumCalculator _premiumCalculator;

    public ComputePremiumQueryHandler(IPremiumCalculator premiumCalculator) =>
        _premiumCalculator = premiumCalculator;

    public Task<Result<decimal>> Handle(ComputePremiumQuery request, CancellationToken cancellationToken)
    {
        var premium = _premiumCalculator.Compute(request.StartDate, request.EndDate, request.CoverType);
        return Task.FromResult(Result<decimal>.Success(premium));
    }
}
