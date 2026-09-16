using Claims.Application.Claims.Commands.CreateClaim;
using Claims.Application.Common.Interfaces;
using Claims.Domain.Common;
using Claims.Domain.Entities;
using Claims.Domain.Enums;
using NSubstitute;

namespace Claims.UnitTests.Application;

public class CreateClaimCommandHandlerTests
{
    private readonly IClaimRepository _claims = Substitute.For<IClaimRepository>();
    private readonly ICoverRepository _covers = Substitute.For<ICoverRepository>();
    private readonly IAuditService _audit = Substitute.For<IAuditService>();

    private static readonly DateOnly CoverStart = new(2027, 1, 1);
    private static readonly DateOnly CoverEnd = new(2027, 6, 30);

    private CreateClaimCommandHandler CreateHandler() => new(_claims, _covers, _audit);

    private static Cover ExistingCover() =>
        Cover.Create(CoverStart, CoverEnd, CoverType.Yacht, new StubPremiumCalculator());

    private static CreateClaimCommand Command(DateOnly created, string coverId = "cover-1") =>
        new(coverId, "Hull damage", ClaimType.Collision, created, 500m);

    [Fact]
    public async Task Returns_not_found_when_the_cover_does_not_exist()
    {
        _covers.GetByIdAsync("cover-1", Arg.Any<CancellationToken>()).Returns((Cover?)null);

        var result = await CreateHandler().Handle(Command(new DateOnly(2027, 2, 1)), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("cover-1", result.Errors.Single());
    }

    [Fact]
    public async Task Does_not_persist_or_audit_when_the_cover_does_not_exist()
    {
        _covers.GetByIdAsync("cover-1", Arg.Any<CancellationToken>()).Returns((Cover?)null);

        await CreateHandler().Handle(Command(new DateOnly(2027, 2, 1)), CancellationToken.None);

        await _claims.DidNotReceive().AddAsync(Arg.Any<Claim>(), Arg.Any<CancellationToken>());
        await _audit.DidNotReceive()
            .AuditClaimAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(2026, 12, 31)]
    [InlineData(2027, 7, 1)]
    public async Task Returns_invalid_when_created_falls_outside_the_cover_period(int y, int m, int d)
    {
        var cover = ExistingCover();
        _covers.GetByIdAsync("cover-1", Arg.Any<CancellationToken>()).Returns(cover);

        var result = await CreateHandler().Handle(Command(new DateOnly(y, m, d)), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains("cover period", result.Errors.Single());
        await _claims.DidNotReceive().AddAsync(Arg.Any<Claim>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(2027, 1, 1)]
    [InlineData(2027, 3, 15)]
    [InlineData(2027, 6, 30)]
    public async Task Accepts_a_created_date_on_or_inside_the_cover_period(int y, int m, int d)
    {
        var cover = ExistingCover();
        _covers.GetByIdAsync("cover-1", Arg.Any<CancellationToken>()).Returns(cover);

        var result = await CreateHandler().Handle(Command(new DateOnly(y, m, d)), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Persists_the_claim_and_returns_it()
    {
        var cover = ExistingCover();
        _covers.GetByIdAsync("cover-1", Arg.Any<CancellationToken>()).Returns(cover);
        var created = new DateOnly(2027, 2, 1);

        var result = await CreateHandler().Handle(Command(created), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("cover-1", result.Value.CoverId);
        Assert.Equal("Hull damage", result.Value.Name);
        Assert.Equal(ClaimType.Collision, result.Value.Type);
        Assert.Equal(created, result.Value.Created);
        Assert.Equal(500m, result.Value.DamageCost);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Id));

        await _claims.Received(1).AddAsync(
            Arg.Is<Claim>(claim => claim.Id == result.Value.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Audits_the_creation_with_the_new_identifier()
    {
        var cover = ExistingCover();
        _covers.GetByIdAsync("cover-1", Arg.Any<CancellationToken>()).Returns(cover);

        var result = await CreateHandler().Handle(Command(new DateOnly(2027, 2, 1)), CancellationToken.None);

        await _audit.Received(1).AuditClaimAsync(result.Value.Id, "POST", Arg.Any<CancellationToken>());
    }
}
