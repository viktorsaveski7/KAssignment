using Claims.Application.Claims.Dtos;
using Claims.Domain.Common;
using MediatR;

namespace Claims.Application.Claims.Queries.GetClaimById;

public record GetClaimByIdQuery(string Id) : IRequest<Result<ClaimDto>>;
