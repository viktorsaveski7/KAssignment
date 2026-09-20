using System.Net.Http.Json;
using Claims.Application.Claims.Dtos;
using Claims.Application.Covers.Dtos;
using Claims.Domain.Enums;

namespace Claims.Tests.Integration;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class AuditTrailTests : IAsyncLifetime
{
    private readonly ClaimsApiFactory _factory;
    private readonly HttpClient _client;

    private static readonly DateOnly CoverStart = new(2030, 1, 1);
    private static readonly DateOnly CoverEnd = new(2030, 6, 30);

    public AuditTrailTests(ClaimsApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async ValueTask InitializeAsync() => await _factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<CoverDto> CreateCoverAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/Covers",
            new { startDate = CoverStart, endDate = CoverEnd, type = nameof(CoverType.Yacht) });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CoverDto>(ClaimsApiFactory.Json))!;
    }

    private async Task<ClaimDto> CreateClaimAsync(string coverId)
    {
        var response = await _client.PostAsJsonAsync("/Claims", new
        {
            coverId,
            name = "Hull",
            type = nameof(ClaimType.Collision),
            created = new DateOnly(2030, 3, 15),
            damageCost = 500m
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClaimDto>(ClaimsApiFactory.Json))!;
    }

    [Fact]
    public async Task Creating_a_cover_is_eventually_audited_as_a_post()
    {
        var cover = await CreateCoverAsync();

        var audits = await _factory.WaitForAuditsAsync(
            context => context.CoverAudits.Where(audit => audit.CoverId == cover.Id),
            expected: 1);

        var audit = Assert.Single(audits);
        Assert.Equal("POST", audit.HttpRequestType);
    }

    [Fact]
    public async Task Deleting_a_cover_is_eventually_audited_as_a_delete()
    {
        var cover = await CreateCoverAsync();

        await _client.DeleteAsync($"/Covers/{cover.Id}");

        var audits = await _factory.WaitForAuditsAsync(
            context => context.CoverAudits.Where(audit => audit.CoverId == cover.Id),
            expected: 2);

        Assert.Equal(2, audits.Count);
        Assert.Contains(audits, audit => audit.HttpRequestType == "POST");
        Assert.Contains(audits, audit => audit.HttpRequestType == "DELETE");
    }

    [Fact]
    public async Task Creating_and_deleting_a_claim_is_eventually_audited()
    {
        var cover = await CreateCoverAsync();
        var claim = await CreateClaimAsync(cover.Id);

        await _client.DeleteAsync($"/Claims/{claim.Id}");

        var audits = await _factory.WaitForAuditsAsync(
            context => context.ClaimAudits.Where(audit => audit.ClaimId == claim.Id),
            expected: 2);

        Assert.Equal(2, audits.Count);
        Assert.Contains(audits, audit => audit.HttpRequestType == "POST");
        Assert.Contains(audits, audit => audit.HttpRequestType == "DELETE");
    }

    [Fact]
    public async Task Audit_timestamps_are_recorded_in_utc()
    {
        var before = DateTime.UtcNow.AddSeconds(-5);

        var cover = await CreateCoverAsync();

        var audits = await _factory.WaitForAuditsAsync(
            context => context.CoverAudits.Where(audit => audit.CoverId == cover.Id),
            expected: 1);

        Assert.InRange(Assert.Single(audits).Created, before, DateTime.UtcNow.AddSeconds(30));
    }

    [Fact]
    public async Task A_failed_delete_is_never_audited()
    {
        await _client.DeleteAsync("/Covers/never-existed");
        await Task.Delay(500);

        var count = await _factory.CountAuditsAsync(
            context => context.CoverAudits.Where(audit => audit.CoverId == "never-existed"));

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task A_rejected_claim_is_never_audited()
    {
        var cover = await CreateCoverAsync();
        var before = await _factory.CountAuditsAsync(context => context.ClaimAudits);

        var response = await _client.PostAsJsonAsync("/Claims", new
        {
            coverId = cover.Id,
            name = "Too expensive",
            type = nameof(ClaimType.Fire),
            created = new DateOnly(2030, 3, 15),
            damageCost = 500_000m
        });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        await Task.Delay(500);

        Assert.Equal(before, await _factory.CountAuditsAsync(context => context.ClaimAudits));
    }
}
