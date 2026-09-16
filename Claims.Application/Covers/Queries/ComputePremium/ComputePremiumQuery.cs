using Claims.Domain.Common;
using Claims.Domain.Enums;
using MediatR;

namespace Claims.Application.Covers.Queries.ComputePremium;

public record ComputePremiumQuery(
    DateOnly StartDate,
    DateOnly EndDate,
    CoverType CoverType) : IRequest<Result<decimal>>;
