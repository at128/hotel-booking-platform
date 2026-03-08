using System.Net;
using FluentAssertions;
using HotelBooking.Api.IntegrationTests.Helpers;
using HotelBooking.Api.IntegrationTests.Infrastructure;
using HotelBooking.Contracts.Hotels;
using HotelBooking.Contracts.Reviews;
using Xunit;

namespace HotelBooking.Api.IntegrationTests.Hotels;

[Collection("Integration")]
public class HotelsTests
{
    private readonly WebAppFactory _factory;
    private readonly HttpClient _client;

    public HotelsTests(WebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHotelDetails_ValidId_ReturnsHotelDetailsDto()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);

        var response = await _client.GetAsync($"/api/v1/hotels/{seed.Hotel.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<HotelDetailsDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be(seed.Hotel.Name);
        result.StarRating.Should().Be(seed.Hotel.StarRating);
    }

    [Fact]
    public async Task GetHotelDetails_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/hotels/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetGallery_WithImages_ReturnsHotelGalleryResponse()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        await SeedHelper.SeedHotelImage(db, seed.Hotel.Id);

        var response = await _client.GetAsync($"/api/v1/hotels/{seed.Hotel.Id}/gallery");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<HotelGalleryResponse>();
        result.Should().NotBeNull();
        result!.Images.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetGallery_NoImages_ReturnsEmptyArray()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);

        var response = await _client.GetAsync($"/api/v1/hotels/{seed.Hotel.Id}/gallery");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<HotelGalleryResponse>();
        result.Should().NotBeNull();
        result!.Images.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailability_ReturnsRoomAvailabilityDtos()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        var response = await _client.GetAsync(
            $"/api/v1/hotels/{seed.Hotel.Id}/room-availability?checkIn={tomorrow:yyyy-MM-dd}&checkOut={dayAfter:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<RoomAvailabilityResponse>();
        result.Should().NotBeNull();
        result!.RoomTypes.Should().NotBeEmpty();
        result.RoomTypes.First().AvailableRooms.Should().Be(5); // 5 rooms seeded
    }

    [Fact]
    public async Task GetAvailability_SomeRoomsBooked_ReturnsCorrectCount()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        // Book one room
        await SeedHelper.SeedConfirmedBooking(db, Guid.NewGuid(), seed.Hotel,
            seed.HotelRoomType, seed.Rooms[0], tomorrow, dayAfter);

        var response = await _client.GetAsync(
            $"/api/v1/hotels/{seed.Hotel.Id}/room-availability?checkIn={tomorrow:yyyy-MM-dd}&checkOut={dayAfter:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<RoomAvailabilityResponse>();
        result.Should().NotBeNull();
        result!.RoomTypes.First().BookedRooms.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetAvailability_AllRoomsBooked_ShowsZeroAvailable()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(11));

        // Book all 5 rooms
        foreach (var room in seed.Rooms)
        {
            await SeedHelper.SeedConfirmedBooking(db, Guid.NewGuid(), seed.Hotel,
                seed.HotelRoomType, room, tomorrow, dayAfter);
        }

        var response = await _client.GetAsync(
            $"/api/v1/hotels/{seed.Hotel.Id}/room-availability?checkIn={tomorrow:yyyy-MM-dd}&checkOut={dayAfter:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<RoomAvailabilityResponse>();
        result.Should().NotBeNull();
        result!.RoomTypes.First().AvailableRooms.Should().Be(0);
    }

    [Fact]
    public async Task GetAvailability_WithActiveHolds_SubtractsFromAvailable()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(21));

        // Create a hold
        var hold = new HotelBooking.Domain.Bookings.CheckoutHold(
            Guid.NewGuid(), Guid.NewGuid(), seed.Hotel.Id, seed.HotelRoomType.Id,
            tomorrow, dayAfter, 2, DateTimeOffset.UtcNow.AddMinutes(10));
        db.CheckoutHolds.Add(hold);
        await db.SaveChangesAsync();

        var response = await _client.GetAsync(
            $"/api/v1/hotels/{seed.Hotel.Id}/room-availability?checkIn={tomorrow:yyyy-MM-dd}&checkOut={dayAfter:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<RoomAvailabilityResponse>();
        result.Should().NotBeNull();
        result!.RoomTypes.First().HeldRooms.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetAvailability_MissingDates_Returns400()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);

        var response = await _client.GetAsync(
            $"/api/v1/hotels/{seed.Hotel.Id}/room-availability");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetHotelReviews_ReturnsPaginatedReviews()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(31));
        var userId = Guid.NewGuid();

        var booking = await SeedHelper.SeedConfirmedBooking(db, userId, seed.Hotel,
            seed.HotelRoomType, seed.Rooms[0], tomorrow, dayAfter);

        // Add a review
        var review = new HotelBooking.Domain.Reviews.Review(
            Guid.NewGuid(), userId, seed.Hotel.Id, booking.Id, 5, "Great", "Excellent stay");
        db.Reviews.Add(review);
        await db.SaveChangesAsync();

        var response = await _client.GetAsync($"/api/v1/hotels/{seed.Hotel.Id}/reviews");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<HotelReviewsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetHotelReviews_EmptyHotel_ReturnsEmptyList()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);

        var response = await _client.GetAsync($"/api/v1/hotels/{seed.Hotel.Id}/reviews");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<HotelReviewsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
    }
}
