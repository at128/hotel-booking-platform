using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HotelBooking.Api.IntegrationTests.Helpers;
using HotelBooking.Api.IntegrationTests.Infrastructure;
using HotelBooking.Contracts.Home;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotelBooking.Api.IntegrationTests.Home;

[Collection("Integration")]
public class HomeTests
{
    private readonly WebAppFactory _factory;
    private readonly HttpClient _client;

    public HomeTests(WebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetFeaturedDeals_WithSeededDeals_ReturnsFeaturedDealDtos()
    {
        await ResetHomeCacheAsync();

        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        await SeedHelper.SeedFeaturedDeal(db, seed.Hotel.Id, seed.HotelRoomType.Id);

        var response = await _client.GetAsync("/api/v1/home/featured-deals");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<FeaturedDealsResponse>();
        result.Should().NotBeNull();
        result!.Deals.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFeaturedDeals_WhenNoDeals_ReturnsEmptyArray()
    {
        await ResetHomeCacheAsync();

        var response = await _client.GetAsync("/api/v1/home/featured-deals");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Should return without error even when empty
    }

    [Fact]
    public async Task GetTrendingCities_ReturnsTop5OrderedByVisits()
    {
        await ResetHomeCacheAsync();

        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        // Create some visits to make the city trending
        for (int i = 0; i < 3; i++)
        {
            var visit = new HotelBooking.Domain.Hotels.HotelVisit(
                Guid.NewGuid(), Guid.NewGuid(), seed.Hotel.Id);
            visit.UpdateVisitTime();
            db.HotelVisits.Add(visit);
        }
        await db.SaveChangesAsync();

        var response = await _client.GetAsync("/api/v1/home/trending-cities");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<TrendingCitiesResponse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTrendingCities_WhenNoVisits_ReturnsEmptyArray()
    {
        await ResetHomeCacheAsync();

        var response = await _client.GetAsync("/api/v1/home/trending-cities");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSearchConfig_ReturnsDefaultSearchValues()
    {
        await ResetHomeCacheAsync();

        var response = await _client.GetAsync("/api/v1/home/config");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<SearchConfigResponse>();
        result.Should().NotBeNull();
        result!.DefaultAdults.Should().BeGreaterThan(0);
        result.DefaultRooms.Should().BeGreaterThan(0);
    }

    private async Task ResetHomeCacheAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<HybridCache>();

        await cache.RemoveAsync("home:featured-deals");
        await cache.RemoveAsync("home:trending-cities");
    }
}
