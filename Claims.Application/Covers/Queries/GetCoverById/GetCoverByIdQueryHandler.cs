using Claims.Application.Common.Interfaces;
using Claims.Application.Covers.Dtos;
using Claims.Domain.Common;
using MediatR;

namespace Claims.Application.Covers.Queries.GetCoverById;

public sealed class GetCoverByIdQueryHandler : IRequestHandler<GetCoverByIdQuery, Result<CoverDto>>
{
    private readonly ICoverRepository _coverRepository;

    public GetCoverByIdQueryHandler(ICoverRepository coverRepository) => _coverRepository = coverRepository;

    public async Task<Result<CoverDto>> Handle(GetCoverByIdQuery request, CancellationToken cancellationToken)
    {
        var cover = await _coverRepository.GetByIdAsync(request.Id, cancellationToken);

        return cover is null
            ? Result<CoverDto>.NotFound($"Cover '{request.Id}' does not exist.")
            : Result<CoverDto>.Success(CoverDto.FromEntity(cover));
    }
}
