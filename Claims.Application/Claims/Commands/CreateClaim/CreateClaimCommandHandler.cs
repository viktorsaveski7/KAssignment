using Claims.Application.Claims.Dtos;
using Claims.Application.Common.Interfaces;
using Claims.Domain.Common;
using Claims.Domain.Entities;
using MediatR;

namespace Claims.Application.Claims.Commands.CreateClaim;

public sealed class CreateClaimCommandHandler : IRequestHandler<CreateClaimCommand, Result<ClaimDto>>
{
    private readonly IClaimRepository _claimRepository;
    private readonly ICoverRepository _coverRepository;
    private readonly IAuditService _auditService;

    public CreateClaimCommandHandler(
        IClaimRepository claimRepository,
        ICoverRepository coverRepository,
        IAuditService auditService)
    {
        _claimRepository = claimRepository;
        _coverRepository = coverRepository;
        _auditService = auditService;
    }

    public async Task<Result<ClaimDto>> Handle(CreateClaimCommand request, CancellationToken cancellationToken)
    {
        var cover = await _coverRepository.GetByIdAsync(request.CoverId, cancellationToken);
        if (cover is null)
        {
            return Result<ClaimDto>.NotFound($"Cover '{request.CoverId}' does not exist.");
        }

        if (!cover.CoversDate(request.Created))
        {
            return Result<ClaimDto>.Invalid(
                $"Created must fall within the cover period " +
                $"({cover.StartDate:yyyy-MM-dd} to {cover.EndDate:yyyy-MM-dd}).");
        }

        var claim = Claim.Create(
            request.CoverId,
            request.Name,
            request.Type,
            request.Created,
            request.DamageCost);

        await _claimRepository.AddAsync(claim, cancellationToken);
        await _auditService.AuditClaimAsync(claim.Id, "POST", cancellationToken);

        return Result<ClaimDto>.Success(ClaimDto.FromEntity(claim));
    }
}
