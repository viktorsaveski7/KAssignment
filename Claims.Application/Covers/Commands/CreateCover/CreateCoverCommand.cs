using Claims.Application.Covers.Dtos;
using Claims.Domain.Common;
using Claims.Domain.Enums;
using MediatR;

namespace Claims.Application.Covers.Commands.CreateCover;

public record CreateCoverCommand(
    DateTime StartDate,
    DateTime EndDate,
    CoverType Type) : IRequest<Result<CoverDto>>;
