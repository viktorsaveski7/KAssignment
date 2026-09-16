using Claims.Application.Common.Interfaces;
using Claims.Domain.Common;
using MediatR;

namespace Claims.Application.Claims.Commands.DeleteClaim;

public sealed class DeleteClaimCommandHandler : IRequestHandler<DeleteClaimCommand, Result>
{
    private readonly IClaimRepository _claimRepository;
    private readonly IAuditService _auditService;

    public DeleteClaimCommandHandler(IClaimRepository claimRepository, IAuditService auditService)
    {
        _claimRepository = claimRepository;
        _auditService = auditService;
    }

    public async Task<Result> Handle(DeleteClaimCommand request, CancellationToken cancellationToken)
    {
        var claim = await _claimRepository.GetByIdAsync(request.Id, cancellationToken);
        if (claim is null)
        {
            return Result.NotFound($"Claim '{request.Id}' does not exist.");
        }

        await _claimRepository.DeleteAsync(claim, cancellationToken);

        await _auditService.AuditClaimAsync(request.Id, "DELETE", cancellationToken);

        return Result.Success();
    }
}
