using Claims.Domain.Common;
using MediatR;

namespace Claims.Application.Claims.Commands.DeleteClaim;

public record DeleteClaimCommand(string Id) : IRequest<Result>;
