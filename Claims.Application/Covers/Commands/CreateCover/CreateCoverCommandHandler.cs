using Claims.Application.Common.Interfaces;
using Claims.Application.Covers.Dtos;
using Claims.Domain.Common;
using Claims.Domain.Entities;
using Claims.Domain.Premium;
using MediatR;

namespace Claims.Application.Covers.Commands.CreateCover;

public sealed class CreateCoverCommandHandler : IRequestHandler<CreateCoverCommand, Result<CoverDto>>
{
    private readonly ICoverRepository _coverRepository;
    private readonly IPremiumCalculator _premiumCalculator;
    private readonly IAuditService _auditService;

    public CreateCoverCommandHandler(
        ICoverRepository coverRepository,
        IPremiumCalculator premiumCalculator,
        IAuditService auditService)
    {
        _coverRepository = coverRepository;
        _premiumCalculator = premiumCalculator;
        _auditService = auditService;
    }

    public async Task<Result<CoverDto>> Handle(CreateCoverCommand request, CancellationToken cancellationToken)
    {
        var cover = Cover.Create(request.StartDate, request.EndDate, request.Type, _premiumCalculator);

        await _coverRepository.AddAsync(cover, cancellationToken);
        await _auditService.AuditCoverAsync(cover.Id, "POST", cancellationToken);

        return Result<CoverDto>.Success(CoverDto.FromEntity(cover));
    }
}
