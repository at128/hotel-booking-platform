using System.Net;
using FluentAssertions;
using HotelBooking.Api.IntegrationTests.Helpers;
using HotelBooking.Api.IntegrationTests.Infrastructure;
using HotelBooking.Contracts.Search;
using Xunit;

namespace HotelBooking.Api.IntegrationTests.Search;

[Collection("Integration")]
public class SearchTests
{
    private readonly WebAppFactory _factory;
    private readonly HttpClient _client;

    public SearchTests(WebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<SeedResult> SeedData()
    {
        using var db = _factory.CreateDbContext();
        return await SeedHelper.SeedFullHierarchy(db);
    }

    [Fact]
    public async Task Search_ByCityName_ReturnsMatchingHotels()
    {
        var seed = await SeedData();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        var response = await _client.GetAsync(
            $"/api/v1/search?City={seed.City.Name}&CheckIn={tomorrow:yyyy-MM-dd}&CheckOut={dayAfter:yyyy-MM-dd}&Adults=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<SearchHotelsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().Contain(h => h.HotelId == seed.Hotel.Id);
    }

    [Fact]
    public async Task Search_ByHotelName_ReturnsMatchingHotel()
    {
        var seed = await SeedData();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        var response = await _client.GetAsync(
            $"/api/v1/search?Query={Uri.EscapeDataString(seed.Hotel.Name)}&CheckIn={tomorrow:yyyy-MM-dd}&CheckOut={dayAfter:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<SearchHotelsResponse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Search_WithDateRange_ExcludesFullyBookedHotels()
    {
        var seed = await SeedData();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        var response = await _client.GetAsync(
            $"/api/v1/search?City={seed.City.Name}&CheckIn={tomorrow:yyyy-MM-dd}&CheckOut={dayAfter:yyyy-MM-dd}&Adults=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Search_WithPriceFilter_ReturnsHotelsInRange()
    {
        var seed = await SeedData();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        var response = await _client.GetAsync(
            $"/api/v1/search?City={seed.City.Name}&CheckIn={tomorrow:yyyy-MM-dd}&CheckOut={dayAfter:yyyy-MM-dd}&MinPrice=100&MaxPrice=200");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<SearchHotelsResponse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Search_WithStarRatingFilter_ReturnsMatchingStars()
    {
        var seed = await SeedData();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        var response = await _client.GetAsync(
            $"/api/v1/search?City={seed.City.Name}&CheckIn={tomorrow:yyyy-MM-dd}&CheckOut={dayAfter:yyyy-MM-dd}&MinStarRating=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<SearchHotelsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().OnlyContain(h => h.StarRating >= 5);
    }

    [Fact]
    public async Task Search_WithMultipleFilters_ReturnsIntersection()
    {
        var seed = await SeedData();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        var response = await _client.GetAsync(
            $"/api/v1/search?City={seed.City.Name}&CheckIn={tomorrow:yyyy-MM-dd}&CheckOut={dayAfter:yyyy-MM-dd}&Adults=2&MinPrice=100&MaxPrice=200&MinStarRating=4");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Search_WithPagination_ReturnsPaginatedResults()
    {
        await SeedData();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        var response = await _client.GetAsync(
            $"/api/v1/search?CheckIn={tomorrow:yyyy-MM-dd}&CheckOut={dayAfter:yyyy-MM-dd}&Limit=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<SearchHotelsResponse>();
        result.Should().NotBeNull();
        result!.Limit.Should().Be(1);
    }

    [Fact]
    public async Task Search_WithSortByPriceAsc_ReturnsSortedResults()
    {
        await SeedData();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        var response = await _client.GetAsync(
            $"/api/v1/search?CheckIn={tomorrow:yyyy-MM-dd}&CheckOut={dayAfter:yyyy-MM-dd}&SortBy=price_asc");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<SearchHotelsResponse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Search_NoResults_ReturnsEmpty()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        var response = await _client.GetAsync(
            $"/api/v1/search?City=NonexistentCityXYZ&CheckIn={tomorrow:yyyy-MM-dd}&CheckOut={dayAfter:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<SearchHotelsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_CheckOutBeforeCheckIn_Returns400()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

        var response = await _client.GetAsync(
            $"/api/v1/search?CheckIn={tomorrow:yyyy-MM-dd}&CheckOut={yesterday:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_CheckInInPast_Returns400()
    {
        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5));
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        var response = await _client.GetAsync(
            $"/api/v1/search?CheckIn={pastDate:yyyy-MM-dd}&CheckOut={futureDate:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_ZeroAdults_Returns400()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        var response = await _client.GetAsync(
            $"/api/v1/search?CheckIn={tomorrow:yyyy-MM-dd}&CheckOut={dayAfter:yyyy-MM-dd}&Adults=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
