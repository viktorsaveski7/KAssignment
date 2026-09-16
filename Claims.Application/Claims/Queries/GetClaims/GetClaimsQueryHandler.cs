using Claims.Application.Claims.Dtos;
using Claims.Application.Common.Interfaces;
using MediatR;

namespace Claims.Application.Claims.Queries.GetClaims;

public sealed class GetClaimsQueryHandler : IRequestHandler<GetClaimsQuery, IReadOnlyList<ClaimDto>>
{
    private readonly IClaimRepository _claimRepository;

    public GetClaimsQueryHandler(IClaimRepository claimRepository) => _claimRepository = claimRepository;

    public async Task<IReadOnlyList<ClaimDto>> Handle(GetClaimsQuery request, CancellationToken cancellationToken)
    {
        var claims = await _claimRepository.GetAllAsync(cancellationToken);
        return claims.Select(ClaimDto.FromEntity).ToList();
    }
}
