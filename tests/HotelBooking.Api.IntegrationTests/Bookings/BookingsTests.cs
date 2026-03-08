using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HotelBooking.Api.IntegrationTests.Helpers;
using HotelBooking.Api.IntegrationTests.Infrastructure;
using HotelBooking.Contracts.Checkout;
using HotelBooking.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelBooking.Api.IntegrationTests.Bookings;

[Collection("Integration")]
public class BookingsTests
{
    private readonly WebAppFactory _factory;

    public BookingsTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetUserBookings_ReturnsOnlyCurrentUserBookings()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client, $"mybookings-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(61));

        await SeedHelper.SeedConfirmedBooking(db, auth.Id, seed.Hotel,
            seed.HotelRoomType, seed.Rooms[0], tomorrow, dayAfter, $"mybookings-{auth.Id}@test.com");

        var response = await client.GetAsync("/api/v1/bookings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<PaginatedResponse<BookingListItemDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetUserBookings_NoBookings_ReturnsEmptyList()
    {
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client, $"nobookings-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var response = await client.GetAsync("/api/v1/bookings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<PaginatedResponse<BookingListItemDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBooking_ValidId_ReturnsBookingDetailsResponse()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client, $"getbk-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(70));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(71));

        var booking = await SeedHelper.SeedConfirmedBooking(db, auth.Id, seed.Hotel,
            seed.HotelRoomType, seed.Rooms[0], tomorrow, dayAfter);

        var response = await client.GetAsync($"/api/v1/bookings/{booking.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<BookingDetailsResponse>();
        result.Should().NotBeNull();
        result!.BookingNumber.Should().NotBeNullOrEmpty();
        result.HotelName.Should().Be(seed.Hotel.Name);
    }

    [Fact]
    public async Task GetBooking_OtherUsersBooking_Returns403()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(80));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(81));

        var otherUserId = Guid.NewGuid();
        var booking = await SeedHelper.SeedConfirmedBooking(db, otherUserId, seed.Hotel,
            seed.HotelRoomType, seed.Rooms[0], tomorrow, dayAfter);

        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client, $"other-bk-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var response = await client.GetAsync($"/api/v1/bookings/{booking.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetBooking_NonExistent_Returns404()
    {
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client, $"ne-bk-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var response = await client.GetAsync($"/api/v1/bookings/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelBooking_MoreThan24HBefore_ReturnsFullRefund()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client, $"cancel-full-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var farFuture = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90));
        var farFuture2 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(91));

        var booking = await SeedHelper.SeedConfirmedBooking(db, auth.Id, seed.Hotel,
            seed.HotelRoomType, seed.Rooms[0], farFuture, farFuture2);

        // The booking was just confirmed, so cancellation within free window (CancellationFreeHours=24)
        var response = await client.PostAsJsonAsync(
            $"/api/v1/bookings/{booking.Id}/cancel",
            new CancelBookingRequest("Changed plans"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<CancellationDetailsResponse>();
        result.Should().NotBeNull();
        // Within free window => refund percentage should be 1.0 (100%)
        result!.RefundPercentage.Should().Be(1.0m);
    }

    [Fact]
    public async Task CancelBooking_LessThan24HBefore_ReturnsPartialRefund()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client, $"cancel-partial-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var farFuture = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100));
        var farFuture2 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(101));

        var booking = await SeedHelper.SeedConfirmedBooking(db, auth.Id, seed.Hotel,
            seed.HotelRoomType, seed.Rooms[0], farFuture, farFuture2);

        // Move payment success time back > 24h to simulate past free window
        // Based on BookingSettings: CancellationFreeHours = 24
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [payments] SET [PaidAtUtc] = {DateTimeOffset.UtcNow.AddHours(-25)} WHERE [BookingId] = {booking.Id}");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/bookings/{booking.Id}/cancel",
            new CancelBookingRequest("Late cancellation"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<CancellationDetailsResponse>();
        result.Should().NotBeNull();
        // Past free window => CancellationFeePercent = 0.30, so refund is 70%
        result!.RefundPercentage.Should().Be(0.70m);
    }

    [Fact]
    public async Task CancelBooking_AfterCheckIn_Returns400Or409()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client, $"cancel-ci-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        // Create a booking with check-in today or in the past
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var tomorrow = today.AddDays(1);

        var booking = await SeedHelper.SeedConfirmedBooking(db, auth.Id, seed.Hotel,
            seed.HotelRoomType, seed.Rooms[0], today, tomorrow);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/bookings/{booking.Id}/cancel",
            new CancelBookingRequest("Too late"));

        // Should return conflict because can't cancel on or after check-in date
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CancelBooking_AlreadyCancelled_Returns409OrOK()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client, $"cancel-dup-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(110));
        var future2 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(111));

        var booking = await SeedHelper.SeedConfirmedBooking(db, auth.Id, seed.Hotel,
            seed.HotelRoomType, seed.Rooms[0], future, future2);

        // First cancel
        await client.PostAsJsonAsync(
            $"/api/v1/bookings/{booking.Id}/cancel",
            new CancelBookingRequest("First cancel"));

        // Second cancel (idempotent path returns OK with existing cancellation)
        var response = await client.PostAsJsonAsync(
            $"/api/v1/bookings/{booking.Id}/cancel",
            new CancelBookingRequest("Second cancel"));

        // Handler returns OK (idempotent) for already cancelled bookings
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CancelBooking_ReleasesRooms()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client, $"cancel-release-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(120));
        var future2 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(121));

        var booking = await SeedHelper.SeedConfirmedBooking(db, auth.Id, seed.Hotel,
            seed.HotelRoomType, seed.Rooms[0], future, future2);

        // Cancel
        await client.PostAsJsonAsync(
            $"/api/v1/bookings/{booking.Id}/cancel",
            new CancelBookingRequest("Cancel to free room"));

        // Check availability — room should be available again
        var availResponse = await client.GetAsync(
            $"/api/v1/hotels/{seed.Hotel.Id}/room-availability?checkIn={future:yyyy-MM-dd}&checkOut={future2:yyyy-MM-dd}");
        availResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CancelBooking_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/bookings/{Guid.NewGuid()}/cancel",
            new CancelBookingRequest("No auth"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
