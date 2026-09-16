using Claims.Application.Covers.Dtos;
using MediatR;

namespace Claims.Application.Covers.Queries.GetCovers;

public record GetCoversQuery : IRequest<IReadOnlyList<CoverDto>>;
