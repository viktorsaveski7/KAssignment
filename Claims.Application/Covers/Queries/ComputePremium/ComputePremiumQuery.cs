using Claims.Domain.Enums;
using MediatR;

namespace Claims.Application.Covers.Queries.ComputePremium;

public record ComputePremiumQuery(
    DateTime StartDate,
    DateTime EndDate,
    CoverType CoverType) : IRequest<decimal>;
