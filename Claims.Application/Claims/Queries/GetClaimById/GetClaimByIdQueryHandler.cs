using Claims.Application.Claims.Dtos;
using Claims.Application.Common.Interfaces;
using Claims.Domain.Common;
using MediatR;

namespace Claims.Application.Claims.Queries.GetClaimById;

public sealed class GetClaimByIdQueryHandler : IRequestHandler<GetClaimByIdQuery, Result<ClaimDto>>
{
    private readonly IClaimRepository _claimRepository;

    public GetClaimByIdQueryHandler(IClaimRepository claimRepository) => _claimRepository = claimRepository;

    public async Task<Result<ClaimDto>> Handle(GetClaimByIdQuery request, CancellationToken cancellationToken)
    {
        var claim = await _claimRepository.GetByIdAsync(request.Id, cancellationToken);

        return claim is null
            ? Result<ClaimDto>.NotFound($"Claim '{request.Id}' does not exist.")
            : Result<ClaimDto>.Success(ClaimDto.FromEntity(claim));
    }
}
