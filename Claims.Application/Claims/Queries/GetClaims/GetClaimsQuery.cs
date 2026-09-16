using Claims.Application.Claims.Dtos;
using MediatR;

namespace Claims.Application.Claims.Queries.GetClaims;

public record GetClaimsQuery : IRequest<IReadOnlyList<ClaimDto>>;
