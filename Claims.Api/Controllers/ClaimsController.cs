using Claims.Api.Common;
using Claims.Application.Claims.Commands.CreateClaim;
using Claims.Application.Claims.Commands.DeleteClaim;
using Claims.Application.Claims.Dtos;
using Claims.Application.Claims.Queries.GetClaimById;
using Claims.Application.Claims.Queries.GetClaims;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class ClaimsController : ControllerBase
{
    private readonly ISender _sender;

    public ClaimsController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ClaimDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClaimDto>>> GetAsync(CancellationToken cancellationToken)
    {
        var claims = await _sender.Send(new GetClaimsQuery(), cancellationToken);
        return Ok(claims);
    }

    [HttpGet("{id}", Name = nameof(GetClaimAsync))]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimDto>> GetClaimAsync(string id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetClaimByIdQuery(id), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimDto>> CreateAsync(
        CreateClaimCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return result.ToCreatedResult(this, nameof(GetClaimAsync), claim => new { id = claim.Id });
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteClaimCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }
}
