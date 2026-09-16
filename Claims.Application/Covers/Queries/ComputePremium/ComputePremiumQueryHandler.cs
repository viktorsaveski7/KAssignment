using Claims.Domain.Premium;
using MediatR;

namespace Claims.Application.Covers.Queries.ComputePremium;

public sealed class ComputePremiumQueryHandler : IRequestHandler<ComputePremiumQuery, decimal>
{
    private readonly IPremiumCalculator _premiumCalculator;

    public ComputePremiumQueryHandler(IPremiumCalculator premiumCalculator) =>
        _premiumCalculator = premiumCalculator;

    public Task<decimal> Handle(ComputePremiumQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(_premiumCalculator.Compute(request.StartDate, request.EndDate, request.CoverType));
}
