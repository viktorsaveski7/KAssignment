using Claims.Domain.Entities;
using Claims.Domain.Enums;

namespace Claims.Application.Claims.Dtos;

public record ClaimDto(
    string Id,
    string CoverId,
    DateOnly Created,
    string Name,
    ClaimType Type,
    decimal DamageCost)
{
    public static ClaimDto FromEntity(Claim claim) =>
        new(claim.Id, claim.CoverId, claim.Created, claim.Name, claim.Type, claim.DamageCost);
}
