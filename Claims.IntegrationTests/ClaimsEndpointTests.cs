using System.Net;
using System.Net.Http.Json;
using Claims.Application.Claims.Dtos;
using Claims.Application.Covers.Dtos;
using Claims.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Claims.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ClaimsEndpointTests : IAsyncLifetime
{
    private readonly ClaimsApiFactory _factory;
    private readonly HttpClient _client;

    private static readonly DateOnly CoverStart = new(2030, 1, 1);
    private static readonly DateOnly CoverEnd = new(2030, 6, 30);
    private static readonly DateOnly InsideCover = new(2030, 3, 15);

    public ClaimsEndpointTests(ClaimsApiFactory factory)
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

    private static object NewClaim(string coverId, DateOnly created, decimal damageCost = 500m, string name = "Hull") =>
        new { coverId, name, type = nameof(ClaimType.Collision), created, damageCost };

    private async Task<ClaimDto> CreateClaimAsync(string coverId)
    {
        var response = await _client.PostAsJsonAsync("/Claims", NewClaim(coverId, InsideCover));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClaimDto>(ClaimsApiFactory.Json))!;
    }

    [Fact]
    public async Task Get_returns_an_empty_list_when_there_are_no_claims()
    {
        var claims = await _client.GetFromJsonAsync<List<ClaimDto>>("/Claims", ClaimsApiFactory.Json);

        Assert.NotNull(claims);
        Assert.Empty(claims);
    }

    [Fact]
    public async Task Post_creates_a_claim_and_returns_201_with_a_location_header()
    {
        var cover = await CreateCoverAsync();

        var response = await _client.PostAsJsonAsync("/Claims", NewClaim(cover.Id, InsideCover));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var claim = await response.Content.ReadFromJsonAsync<ClaimDto>(ClaimsApiFactory.Json);
        Assert.Equal(cover.Id, claim!.CoverId);
        Assert.Equal(InsideCover, claim.Created);
        Assert.Equal(500m, claim.DamageCost);
    }

    [Fact]
    public async Task The_location_header_resolves_to_the_created_claim()
    {
        var cover = await CreateCoverAsync();
        var response = await _client.PostAsJsonAsync("/Claims", NewClaim(cover.Id, InsideCover));
        var created = await response.Content.ReadFromJsonAsync<ClaimDto>(ClaimsApiFactory.Json);

        var followed = await _client.GetFromJsonAsync<ClaimDto>(
            response.Headers.Location!.PathAndQuery,
            ClaimsApiFactory.Json);

        Assert.Equal(created!.Id, followed!.Id);
    }

    [Fact]
    public async Task Post_against_an_unknown_cover_returns_404()
    {
        var response = await _client.PostAsJsonAsync("/Claims", NewClaim("no-such-cover", InsideCover));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Contains("no-such-cover", problem!.Detail);
    }

    [Fact]
    public async Task Post_rejects_a_damage_cost_above_the_limit()
    {
        var cover = await CreateCoverAsync();

        var response = await _client.PostAsJsonAsync(
            "/Claims",
            NewClaim(cover.Id, InsideCover, damageCost: 100_001m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Contains("cannot exceed", problem!.Detail);
    }

    [Fact]
    public async Task Post_accepts_a_damage_cost_exactly_at_the_limit()
    {
        var cover = await CreateCoverAsync();

        var response = await _client.PostAsJsonAsync(
            "/Claims",
            NewClaim(cover.Id, InsideCover, damageCost: 100_000m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData(2029, 12, 31)]
    [InlineData(2030, 7, 1)]
    public async Task Post_rejects_a_created_date_outside_the_cover_period(int year, int month, int day)
    {
        var cover = await CreateCoverAsync();

        var response = await _client.PostAsJsonAsync(
            "/Claims",
            NewClaim(cover.Id, new DateOnly(year, month, day)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Contains("cover period", problem!.Detail);
    }

    [Theory]
    [InlineData(2030, 1, 1)]
    [InlineData(2030, 3, 15)]
    [InlineData(2030, 6, 30)]
    public async Task Post_accepts_a_created_date_on_the_cover_boundaries(int year, int month, int day)
    {
        var cover = await CreateCoverAsync();

        var response = await _client.PostAsJsonAsync(
            "/Claims",
            NewClaim(cover.Id, new DateOnly(year, month, day)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_rejects_a_missing_name()
    {
        var cover = await CreateCoverAsync();

        var response = await _client.PostAsJsonAsync(
            "/Claims",
            NewClaim(cover.Id, InsideCover, name: ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_by_unknown_id_returns_a_404_problem_document()
    {
        var response = await _client.GetAsync("/Claims/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(404, problem!.Status);
    }

    [Fact]
    public async Task Get_returns_every_created_claim()
    {
        var cover = await CreateCoverAsync();
        await CreateClaimAsync(cover.Id);
        await CreateClaimAsync(cover.Id);

        var claims = await _client.GetFromJsonAsync<List<ClaimDto>>("/Claims", ClaimsApiFactory.Json);

        Assert.Equal(2, claims!.Count);
    }

    [Fact]
    public async Task Delete_removes_the_claim_and_returns_204()
    {
        var cover = await CreateCoverAsync();
        var claim = await CreateClaimAsync(cover.Id);

        var response = await _client.DeleteAsync($"/Claims/{claim.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/Claims/{claim.Id}")).StatusCode);
    }

    [Fact]
    public async Task Deleting_a_claim_twice_returns_404_the_second_time()
    {
        var cover = await CreateCoverAsync();
        var claim = await CreateClaimAsync(cover.Id);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/Claims/{claim.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/Claims/{claim.Id}")).StatusCode);
    }

    [Fact]
    public async Task Readiness_reports_healthy_and_names_both_database_checks()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var report = await response.Content.ReadFromJsonAsync<HealthReportResponse>(ClaimsApiFactory.Json);

        Assert.Equal("Healthy", report!.Status);
        Assert.Equal(2, report.Checks.Count);
        Assert.Contains(report.Checks, check => check.Name == "audit-database" && check.Status == "Healthy");
        Assert.Contains(report.Checks, check => check.Name == "claims-database" && check.Status == "Healthy");
    }

    [Fact]
    public async Task Liveness_reports_healthy_and_checks_no_dependencies()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var report = await response.Content.ReadFromJsonAsync<HealthReportResponse>(ClaimsApiFactory.Json);

        Assert.Equal("Healthy", report!.Status);
        Assert.Empty(report.Checks);
    }

    private sealed record HealthReportResponse(string Status, List<HealthCheckEntry> Checks);

    private sealed record HealthCheckEntry(string Name, string Status, string? Description);
}
