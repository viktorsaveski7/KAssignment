using Claims.Application.Common.Interfaces;
using Claims.Domain.Common;
using MediatR;

namespace Claims.Application.Covers.Commands.DeleteCover;

public sealed class DeleteCoverCommandHandler : IRequestHandler<DeleteCoverCommand, Result>
{
    private readonly ICoverRepository _coverRepository;
    private readonly IAuditService _auditService;

    public DeleteCoverCommandHandler(ICoverRepository coverRepository, IAuditService auditService)
    {
        _coverRepository = coverRepository;
        _auditService = auditService;
    }

    public async Task<Result> Handle(DeleteCoverCommand request, CancellationToken cancellationToken)
    {
        var cover = await _coverRepository.GetByIdAsync(request.Id, cancellationToken);
        if (cover is null)
        {
            return Result.NotFound($"Cover '{request.Id}' does not exist.");
        }

        await _coverRepository.DeleteAsync(cover, cancellationToken);
        await _auditService.AuditCoverAsync(request.Id, "DELETE", cancellationToken);

        return Result.Success();
    }
}
