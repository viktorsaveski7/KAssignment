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

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class CoversController : ControllerBase
{
    private readonly ISender _sender;

    public CoversController(ISender sender) => _sender = sender;

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

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CoverDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CoverDto>>> GetAsync(CancellationToken cancellationToken)
    {
        var covers = await _sender.Send(new GetCoversQuery(), cancellationToken);
        return Ok(covers);
    }

    [HttpGet("{id}", Name = nameof(GetCoverAsync))]
    [ProducesResponseType(typeof(CoverDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CoverDto>> GetCoverAsync(string id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCoverByIdQuery(id), cancellationToken);
        return result.ToActionResult(this);
    }

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

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteCoverCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }
}
