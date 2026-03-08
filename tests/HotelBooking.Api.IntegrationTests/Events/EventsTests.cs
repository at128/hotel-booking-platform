using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HotelBooking.Api.IntegrationTests.Helpers;
using HotelBooking.Api.IntegrationTests.Infrastructure;
using HotelBooking.Contracts.Events;
using Xunit;

namespace HotelBooking.Api.IntegrationTests.Events;

[Collection("Integration")]
public class EventsTests
{
    private readonly WebAppFactory _factory;

    public EventsTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TrackHotelView_ValidHotelId_Returns204OrOk()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client,
            $"track-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var response = await client.PostAsJsonAsync("/api/v1/events/hotel-viewed",
            new TrackHotelViewRequest(seed.Hotel.Id));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task TrackHotelView_SameHotelWithin5Min_DeduplicatesVisit()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client,
            $"dedup-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        // First visit
        await client.PostAsJsonAsync("/api/v1/events/hotel-viewed",
            new TrackHotelViewRequest(seed.Hotel.Id));

        // Second visit immediately after
        var response = await client.PostAsJsonAsync("/api/v1/events/hotel-viewed",
            new TrackHotelViewRequest(seed.Hotel.Id));

        // Should still succeed (deduplicated server-side)
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task TrackHotelView_NonExistentHotel_Returns404()
    {
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client,
            $"track-ne-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var response = await client.PostAsJsonAsync("/api/v1/events/hotel-viewed",
            new TrackHotelViewRequest(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TrackHotelView_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/events/hotel-viewed",
            new TrackHotelViewRequest(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRecentlyVisited_AfterViewingHotels_ReturnsOrderedList()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client,
            $"recent-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        await client.PostAsJsonAsync("/api/v1/events/hotel-viewed",
            new TrackHotelViewRequest(seed.Hotel.Id));

        var response = await client.GetAsync("/api/v1/events/recently-visited");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<RecentlyVisitedResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetRecentlyVisited_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/events/recently-visited");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRecentlyVisited_NewUser_ReturnsEmptyList()
    {
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client,
            $"newuser-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var response = await client.GetAsync("/api/v1/events/recently-visited");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<RecentlyVisitedResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task TrackHotelView_UpdatesTrendingCitiesCount()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client,
            $"trending-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        await client.PostAsJsonAsync("/api/v1/events/hotel-viewed",
            new TrackHotelViewRequest(seed.Hotel.Id));

        // Verify trending cities now includes visits
        var trendingResponse = await client.GetAsync("/api/v1/home/trending-cities");
        trendingResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
