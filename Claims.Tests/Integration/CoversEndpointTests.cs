using System.Net;
using System.Net.Http.Json;
using Claims.Application.Covers.Dtos;
using Claims.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Tests.Integration;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class CoversEndpointTests : IAsyncLifetime
{
    private readonly ClaimsApiFactory _factory;
    private readonly HttpClient _client;

    private static readonly DateOnly Start = new(2030, 1, 1);
    private static readonly DateOnly End = new(2030, 6, 30);

    public CoversEndpointTests(ClaimsApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async ValueTask InitializeAsync() => await _factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static object NewCover(DateOnly start, DateOnly end, CoverType type = CoverType.Yacht) =>
        new { startDate = start, endDate = end, type = type.ToString() };

    private async Task<CoverDto> CreateCoverAsync()
    {
        var response = await _client.PostAsJsonAsync("/Covers", NewCover(Start, End));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CoverDto>(ClaimsApiFactory.Json))!;
    }

    [Fact]
    public async Task Get_returns_an_empty_list_when_there_are_no_covers()
    {
        var covers = await _client.GetFromJsonAsync<List<CoverDto>>("/Covers", ClaimsApiFactory.Json);

        Assert.NotNull(covers);
        Assert.Empty(covers);
    }

    [Fact]
    public async Task Post_creates_a_cover_and_returns_201_with_a_location_header()
    {
        var response = await _client.PostAsJsonAsync("/Covers", NewCover(Start, End));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var cover = await response.Content.ReadFromJsonAsync<CoverDto>(ClaimsApiFactory.Json);
        Assert.NotNull(cover);
        Assert.False(string.IsNullOrWhiteSpace(cover.Id));
        Assert.Contains(cover.Id, response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task The_location_header_resolves_to_the_created_cover()
    {
        var response = await _client.PostAsJsonAsync("/Covers", NewCover(Start, End));
        var created = await response.Content.ReadFromJsonAsync<CoverDto>(ClaimsApiFactory.Json);

        var followed = await _client.GetFromJsonAsync<CoverDto>(
            response.Headers.Location!.PathAndQuery,
            ClaimsApiFactory.Json);

        Assert.Equal(created!.Id, followed!.Id);
    }

    [Fact]
    public async Task Dates_survive_the_round_trip_without_shifting()
    {
        var created = await CreateCoverAsync();

        var fetched = await _client.GetFromJsonAsync<CoverDto>($"/Covers/{created.Id}", ClaimsApiFactory.Json);

        Assert.Equal(Start, created.StartDate);
        Assert.Equal(End, created.EndDate);
        Assert.Equal(Start, fetched!.StartDate);
        Assert.Equal(End, fetched.EndDate);
    }

    [Fact]
    public async Task The_premium_is_computed_by_the_server_and_ignores_any_supplied_value()
    {
        var response = await _client.PostAsJsonAsync("/Covers", new
        {
            startDate = Start,
            endDate = End,
            type = nameof(CoverType.Yacht),
            premium = 1m,
            id = "client-chosen-id"
        });

        var cover = await response.Content.ReadFromJsonAsync<CoverDto>(ClaimsApiFactory.Json);

        Assert.NotEqual(1m, cover!.Premium);
        Assert.NotEqual("client-chosen-id", cover.Id);
        Assert.True(cover.Premium > 0m);
    }

    [Fact]
    public async Task Get_by_id_returns_the_cover()
    {
        var created = await CreateCoverAsync();

        var cover = await _client.GetFromJsonAsync<CoverDto>($"/Covers/{created.Id}", ClaimsApiFactory.Json);

        Assert.Equal(created.Id, cover!.Id);
        Assert.Equal(created.Premium, cover.Premium);
        Assert.Equal(CoverType.Yacht, cover.Type);
    }

    [Fact]
    public async Task Get_by_unknown_id_returns_a_404_problem_document()
    {
        var response = await _client.GetAsync("/Covers/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(404, problem!.Status);
        Assert.Contains("does-not-exist", problem.Detail);
    }

    [Fact]
    public async Task Get_returns_every_created_cover()
    {
        await CreateCoverAsync();
        await CreateCoverAsync();

        var covers = await _client.GetFromJsonAsync<List<CoverDto>>("/Covers", ClaimsApiFactory.Json);

        Assert.Equal(2, covers!.Count);
    }

    [Fact]
    public async Task Delete_removes_the_cover_and_returns_204()
    {
        var created = await CreateCoverAsync();

        var response = await _client.DeleteAsync($"/Covers/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/Covers/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Deleting_a_cover_twice_returns_404_the_second_time()
    {
        var created = await CreateCoverAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/Covers/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/Covers/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Post_rejects_a_start_date_in_the_past()
    {
        var response = await _client.PostAsJsonAsync(
            "/Covers",
            NewCover(new DateOnly(2020, 1, 1), new DateOnly(2020, 6, 30)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Contains("cannot be in the past", problem!.Detail);
    }

    [Fact]
    public async Task Post_rejects_a_period_longer_than_a_year()
    {
        var response = await _client.PostAsJsonAsync(
            "/Covers",
            NewCover(new DateOnly(2030, 1, 1), new DateOnly(2031, 1, 1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Contains("exceed one year", problem!.Detail);
    }

    [Fact]
    public async Task Post_accepts_a_period_of_exactly_one_year()
    {
        var response = await _client.PostAsJsonAsync(
            "/Covers",
            NewCover(new DateOnly(2030, 1, 1), new DateOnly(2030, 12, 31)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_rejects_an_end_date_before_the_start_date()
    {
        var response = await _client.PostAsJsonAsync(
            "/Covers",
            NewCover(new DateOnly(2030, 6, 30), new DateOnly(2030, 1, 1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Compute_prices_a_period_without_creating_a_cover()
    {
        var premium = await _client.GetFromJsonAsync<decimal>(
            $"/Covers/compute?startDate={Start:yyyy-MM-dd}&endDate={End:yyyy-MM-dd}&coverType=Yacht");

        Assert.True(premium > 0m);

        var covers = await _client.GetFromJsonAsync<List<CoverDto>>("/Covers", ClaimsApiFactory.Json);
        Assert.Empty(covers!);
    }

    [Fact]
    public async Task Compute_rejects_a_reversed_period()
    {
        var response = await _client.GetAsync(
            $"/Covers/compute?startDate={End:yyyy-MM-dd}&endDate={Start:yyyy-MM-dd}&coverType=Yacht");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Compute_matches_the_premium_charged_when_the_cover_is_created()
    {
        var quoted = await _client.GetFromJsonAsync<decimal>(
            $"/Covers/compute?startDate={Start:yyyy-MM-dd}&endDate={End:yyyy-MM-dd}&coverType=Yacht");

        var created = await CreateCoverAsync();

        Assert.Equal(quoted, created.Premium);
    }
}
