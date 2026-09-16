using Claims.Api.Common;
using Claims.Application.Covers.Commands.CreateCover;
using Claims.Application.Covers.Commands.DeleteCover;
using Claims.Application.Covers.Dtos;
using Claims.Application.Covers.Queries.ComputePremium;
using Claims.Application.Covers.Queries.GetCoverById;
using Claims.Application.Covers.Queries.GetCovers;
using Claims.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Api.Controllers;

/// <summary>Insurance covers taken out on a vessel for a fixed period.</summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class CoversController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Creates the controller.</summary>
    public CoversController(ISender sender) => _sender = sender;

    /// <summary>Prices a cover without taking it out.</summary>
    /// <response code="200">The premium for the requested period.</response>
    /// <response code="400">The requested period is not valid.</response>
    [HttpGet("compute")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<decimal>> ComputePremiumAsync(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] CoverType coverType,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ComputePremiumQuery(startDate, endDate, coverType),
            cancellationToken);

        return result.ToActionResult(this);
    }

    /// <summary>Returns every cover.</summary>
    /// <response code="200">The full list of covers.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CoverDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CoverDto>>> GetAsync(CancellationToken cancellationToken)
    {
        var covers = await _sender.Send(new GetCoversQuery(), cancellationToken);
        return Ok(covers);
    }

    /// <summary>Returns a single cover.</summary>
    /// <response code="200">The requested cover.</response>
    /// <response code="404">No cover exists with that identifier.</response>
    [HttpGet("{id}", Name = nameof(GetCoverAsync))]
    [ProducesResponseType(typeof(CoverDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CoverDto>> GetCoverAsync(string id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCoverByIdQuery(id), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Takes out a new cover. The premium is computed server-side; any supplied value is ignored.</summary>
    /// <response code="201">The cover was created.</response>
    /// <response code="400">The cover violates a business rule.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CoverDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CoverDto>> CreateAsync(
        CreateCoverCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return result.ToCreatedResult(this, nameof(GetCoverAsync), cover => new { id = cover.Id });
    }

    /// <summary>Deletes a cover.</summary>
    /// <response code="204">The cover was deleted.</response>
    /// <response code="404">No cover exists with that identifier.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteCoverCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }
}
