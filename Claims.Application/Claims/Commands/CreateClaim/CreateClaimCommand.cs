using Claims.Application.Claims.Dtos;
using Claims.Domain.Common;
using Claims.Domain.Enums;
using MediatR;

namespace Claims.Application.Claims.Commands.CreateClaim;

public record CreateClaimCommand(
    string CoverId,
    string Name,
    ClaimType Type,
    DateTime Created,
    decimal DamageCost) : IRequest<Result<ClaimDto>>;
