using Claims.Application.Covers.Dtos;
using Claims.Domain.Common;
using MediatR;

namespace Claims.Application.Covers.Queries.GetCoverById;

public record GetCoverByIdQuery(string Id) : IRequest<Result<CoverDto>>;
