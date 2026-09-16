using Claims.Api.Common;
using Claims.Application.Claims.Commands.CreateClaim;
using Claims.Application.Claims.Commands.DeleteClaim;
using Claims.Application.Claims.Dtos;
using Claims.Application.Claims.Queries.GetClaimById;
using Claims.Application.Claims.Queries.GetClaims;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Api.Controllers;

/// <summary>Claims filed against an insurance cover.</summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class ClaimsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Creates the controller.</summary>
    public ClaimsController(ISender sender) => _sender = sender;

    /// <summary>Returns every claim.</summary>
    /// <response code="200">The full list of claims.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ClaimDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClaimDto>>> GetAsync(CancellationToken cancellationToken)
    {
        var claims = await _sender.Send(new GetClaimsQuery(), cancellationToken);
        return Ok(claims);
    }

    /// <summary>Returns a single claim.</summary>
    /// <response code="200">The requested claim.</response>
    /// <response code="404">No claim exists with that identifier.</response>
    [HttpGet("{id}", Name = nameof(GetClaimAsync))]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimDto>> GetClaimAsync(string id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetClaimByIdQuery(id), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Files a new claim against an existing cover.</summary>
    /// <response code="201">The claim was created.</response>
    /// <response code="400">The claim violates a business rule.</response>
    /// <response code="404">The referenced cover does not exist.</response>
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

    /// <summary>Deletes a claim.</summary>
    /// <response code="204">The claim was deleted.</response>
    /// <response code="404">No claim exists with that identifier.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteClaimCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }
}
