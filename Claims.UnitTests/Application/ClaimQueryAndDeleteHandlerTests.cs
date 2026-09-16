using Claims.Application.Claims.Commands.DeleteClaim;
using Claims.Application.Claims.Queries.GetClaimById;
using Claims.Application.Claims.Queries.GetClaims;
using Claims.Application.Common.Interfaces;
using Claims.Domain.Common;
using Claims.Domain.Entities;
using Claims.Domain.Enums;
using NSubstitute;

namespace Claims.UnitTests.Application;

public class ClaimQueryAndDeleteHandlerTests
{
    private readonly IClaimRepository _claims = Substitute.For<IClaimRepository>();
    private readonly IAuditService _audit = Substitute.For<IAuditService>();

    private static Claim SampleClaim() =>
        Claim.Create("cover-1", "Hull damage", ClaimType.Collision, new DateOnly(2027, 2, 1), 500m);

    [Fact]
    public async Task GetClaims_projects_every_claim()
    {
        var one = SampleClaim();
        var two = SampleClaim();
        _claims.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { one, two });

        var result = await new GetClaimsQueryHandler(_claims)
            .Handle(new GetClaimsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { one.Id, two.Id }, result.Select(claim => claim.Id));
    }

    [Fact]
    public async Task GetClaims_returns_an_empty_list_when_there_are_none()
    {
        _claims.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Claim>());

        var result = await new GetClaimsQueryHandler(_claims)
            .Handle(new GetClaimsQuery(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetClaimById_returns_the_claim_when_it_exists()
    {
        var claim = SampleClaim();
        _claims.GetByIdAsync(claim.Id, Arg.Any<CancellationToken>()).Returns(claim);

        var result = await new GetClaimByIdQueryHandler(_claims)
            .Handle(new GetClaimByIdQuery(claim.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(claim.Id, result.Value.Id);
        Assert.Equal(claim.DamageCost, result.Value.DamageCost);
    }

    [Fact]
    public async Task GetClaimById_returns_not_found_when_it_does_not_exist()
    {
        _claims.GetByIdAsync("nope", Arg.Any<CancellationToken>()).Returns((Claim?)null);

        var result = await new GetClaimByIdQueryHandler(_claims)
            .Handle(new GetClaimByIdQuery("nope"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("nope", result.Errors.Single());
    }

    [Fact]
    public async Task DeleteClaim_returns_not_found_and_does_not_audit_when_absent()
    {
        _claims.GetByIdAsync("nope", Arg.Any<CancellationToken>()).Returns((Claim?)null);

        var result = await new DeleteClaimCommandHandler(_claims, _audit)
            .Handle(new DeleteClaimCommand("nope"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
        await _claims.DidNotReceive().DeleteAsync(Arg.Any<Claim>(), Arg.Any<CancellationToken>());
        await _audit.DidNotReceive()
            .AuditClaimAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteClaim_removes_the_claim_and_audits_it()
    {
        var claim = SampleClaim();
        _claims.GetByIdAsync(claim.Id, Arg.Any<CancellationToken>()).Returns(claim);

        var result = await new DeleteClaimCommandHandler(_claims, _audit)
            .Handle(new DeleteClaimCommand(claim.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _claims.Received(1).DeleteAsync(claim, Arg.Any<CancellationToken>());
        await _audit.Received(1).AuditClaimAsync(claim.Id, "DELETE", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteClaim_audits_only_after_the_delete_succeeds()
    {
        var claim = SampleClaim();
        _claims.GetByIdAsync(claim.Id, Arg.Any<CancellationToken>()).Returns(claim);
        _claims.DeleteAsync(claim, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("storage is down")));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new DeleteClaimCommandHandler(_claims, _audit)
                .Handle(new DeleteClaimCommand(claim.Id), CancellationToken.None));

        await _audit.DidNotReceive()
            .AuditClaimAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
