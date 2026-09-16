using Claims.Domain.Common;
using MediatR;

namespace Claims.Application.Covers.Commands.DeleteCover;

public record DeleteCoverCommand(string Id) : IRequest<Result>;
