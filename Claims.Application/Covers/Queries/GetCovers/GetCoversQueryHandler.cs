using Claims.Application.Common.Interfaces;
using Claims.Application.Covers.Dtos;
using MediatR;

namespace Claims.Application.Covers.Queries.GetCovers;

public sealed class GetCoversQueryHandler : IRequestHandler<GetCoversQuery, IReadOnlyList<CoverDto>>
{
    private readonly ICoverRepository _coverRepository;

    public GetCoversQueryHandler(ICoverRepository coverRepository) => _coverRepository = coverRepository;

    public async Task<IReadOnlyList<CoverDto>> Handle(GetCoversQuery request, CancellationToken cancellationToken)
    {
        var covers = await _coverRepository.GetAllAsync(cancellationToken);
        return covers.Select(CoverDto.FromEntity).ToList();
    }
}
