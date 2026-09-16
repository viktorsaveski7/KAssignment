using Claims.Application.Common.Interfaces;
using Claims.Application.Covers.Commands.CreateCover;
using Claims.Application.Covers.Commands.DeleteCover;
using Claims.Application.Covers.Queries.ComputePremium;
using Claims.Application.Covers.Queries.GetCoverById;
using Claims.Application.Covers.Queries.GetCovers;
using Claims.Domain.Common;
using Claims.Domain.Entities;
using Claims.Domain.Enums;
using NSubstitute;

namespace Claims.UnitTests.Application;

public class CoverHandlerTests
{
    private readonly ICoverRepository _covers = Substitute.For<ICoverRepository>();
    private readonly IAuditService _audit = Substitute.For<IAuditService>();
    private readonly StubPremiumCalculator _calculator = new(1_000m);

    private static readonly DateOnly Start = new(2027, 1, 1);
    private static readonly DateOnly End = new(2027, 6, 30);

    private Cover SampleCover() => Cover.Create(Start, End, CoverType.Yacht, _calculator);

    [Fact]
    public async Task CreateCover_persists_the_cover_and_returns_it()
    {
        var result = await new CreateCoverCommandHandler(_covers, _calculator, _audit)
            .Handle(new CreateCoverCommand(Start, End, CoverType.Tanker), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Start, result.Value.StartDate);
        Assert.Equal(End, result.Value.EndDate);
        Assert.Equal(CoverType.Tanker, result.Value.Type);
        await _covers.Received(1).AddAsync(Arg.Any<Cover>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCover_prices_the_cover_using_the_calculator()
    {
        var result = await new CreateCoverCommandHandler(_covers, _calculator, _audit)
            .Handle(new CreateCoverCommand(Start, End, CoverType.Tanker), CancellationToken.None);

        Assert.Equal(1_000m, result.Value.Premium);
        Assert.Equal(Start, _calculator.LastStartDate);
        Assert.Equal(End, _calculator.LastEndDate);
        Assert.Equal(CoverType.Tanker, _calculator.LastCoverType);
    }

    [Fact]
    public async Task CreateCover_audits_the_creation()
    {
        var result = await new CreateCoverCommandHandler(_covers, _calculator, _audit)
            .Handle(new CreateCoverCommand(Start, End, CoverType.Tanker), CancellationToken.None);

        await _audit.Received(1).AuditCoverAsync(result.Value.Id, "POST", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCovers_projects_every_cover()
    {
        var one = SampleCover();
        var two = SampleCover();
        _covers.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { one, two });

        var result = await new GetCoversQueryHandler(_covers)
            .Handle(new GetCoversQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { one.Id, two.Id }, result.Select(cover => cover.Id));
    }

    [Fact]
    public async Task GetCoverById_returns_the_cover_when_it_exists()
    {
        var cover = SampleCover();
        _covers.GetByIdAsync(cover.Id, Arg.Any<CancellationToken>()).Returns(cover);

        var result = await new GetCoverByIdQueryHandler(_covers)
            .Handle(new GetCoverByIdQuery(cover.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(cover.Id, result.Value.Id);
        Assert.Equal(cover.Premium, result.Value.Premium);
    }

    [Fact]
    public async Task GetCoverById_returns_not_found_when_it_does_not_exist()
    {
        _covers.GetByIdAsync("nope", Arg.Any<CancellationToken>()).Returns((Cover?)null);

        var result = await new GetCoverByIdQueryHandler(_covers)
            .Handle(new GetCoverByIdQuery("nope"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task DeleteCover_returns_not_found_and_does_not_audit_when_absent()
    {
        _covers.GetByIdAsync("nope", Arg.Any<CancellationToken>()).Returns((Cover?)null);

        var result = await new DeleteCoverCommandHandler(_covers, _audit)
            .Handle(new DeleteCoverCommand("nope"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
        await _audit.DidNotReceive()
            .AuditCoverAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteCover_removes_the_cover_and_audits_it()
    {
        var cover = SampleCover();
        _covers.GetByIdAsync(cover.Id, Arg.Any<CancellationToken>()).Returns(cover);

        var result = await new DeleteCoverCommandHandler(_covers, _audit)
            .Handle(new DeleteCoverCommand(cover.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _covers.Received(1).DeleteAsync(cover, Arg.Any<CancellationToken>());
        await _audit.Received(1).AuditCoverAsync(cover.Id, "DELETE", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ComputePremium_delegates_to_the_calculator()
    {
        var calculator = new StubPremiumCalculator(7_777m);

        var result = await new ComputePremiumQueryHandler(calculator)
            .Handle(new ComputePremiumQuery(Start, End, CoverType.PassengerShip), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(7_777m, result.Value);
        Assert.Equal(CoverType.PassengerShip, calculator.LastCoverType);
    }
}
