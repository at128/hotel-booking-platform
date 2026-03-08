using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using HotelBooking.Api.IntegrationTests.Helpers;
using HotelBooking.Api.IntegrationTests.Infrastructure;
using HotelBooking.Application.Common.Models.Payment;
using HotelBooking.Contracts.Auth;
using HotelBooking.Contracts.Cart;
using HotelBooking.Contracts.Checkout;
using HotelBooking.Contracts.Events;
using HotelBooking.Contracts.Hotels;
using HotelBooking.Contracts.Search;
using Xunit;

namespace HotelBooking.Api.IntegrationTests.Workflows;

[Collection("Integration")]
public class FullBookingFlowTests
{
    private readonly WebAppFactory _factory;

    public FullBookingFlowTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullFlow_RegisterToBookingConfirmation_Succeeds()
    {
        // === ARRANGE ===
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        // === 1. Register + Login ===
        var auth = await AuthHelper.RegisterAndLogin(client,
            $"fullflow-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        // === 2. Search ===
        var searchResponse = await client.GetAsync(
            $"/api/v1/search?City={seed.City.Name}&CheckIn={tomorrow:yyyy-MM-dd}&CheckOut={dayAfter:yyyy-MM-dd}&Adults=2");
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // === 3. Track Hotel View ===
        var trackResponse = await client.PostAsJsonAsync("/api/v1/events/hotel-viewed",
            new TrackHotelViewRequest(seed.Hotel.Id));
        trackResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // === 4. Get Availability ===
        var availResponse = await client.GetAsync(
            $"/api/v1/hotels/{seed.Hotel.Id}/room-availability?checkIn={tomorrow:yyyy-MM-dd}&checkOut={dayAfter:yyyy-MM-dd}");
        availResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // === 5. Add to Cart ===
        var addCartResponse = await client.PostAsJsonAsync("/api/v1/cart/items",
            new AddToCartRequest(seed.HotelRoomType.Id, tomorrow, dayAfter, 1, 2, 0));
        addCartResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // === 6. Create Checkout Hold ===
        var holdResponse = await client.PostAsJsonAsync("/api/v1/checkout/hold",
            new CreateHoldRequest(null));
        holdResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var hold = await holdResponse.ReadJsonAsync<CheckoutHoldResponse>();

        // === 7. Create Booking ===
        var bookingResponse = await client.PostAsJsonAsync("/api/v1/checkout/booking",
            new CreateBookingRequest(hold!.HoldIds, "Full flow test"));
        bookingResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var booking = await bookingResponse.ReadJsonAsync<CreateBookingResponse>();
        booking.Should().NotBeNull();
        booking!.BookingId.Should().NotBeEmpty();

        // === 8. Simulate Stripe Webhook ===
        _factory.FakePaymentGateway.NextWebhookParseResult = new WebhookParseResult(
            IsSignatureValid: true,
            EventType: PaymentEventTypes.PaymentSucceeded,
            ProviderSessionId: _factory.FakePaymentGateway.CreatedSessions.Last().BookingNumber,
            TransactionRef: "txn_fullflow_success",
            RawPayload: "{}");

        var webhookClient = _factory.CreateClient();
        var whRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/stripe")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        whRequest.Headers.Add("Stripe-Signature", "test_sig");
        await webhookClient.SendAsync(whRequest);

        // === 9. Verify Booking ===
        var detailsResponse = await client.GetAsync($"/api/v1/bookings/{booking.BookingId}");
        detailsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var details = await detailsResponse.ReadJsonAsync<BookingDetailsResponse>();

        // === ASSERT ===
        details.Should().NotBeNull();
        details!.BookingNumber.Should().NotBeNullOrEmpty();
        details.HotelName.Should().Be(seed.Hotel.Name);
    }

    [Fact]
    public async Task FullFlow_BookingDetails_ContainsAllRequiredFields()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client,
            $"details-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(170));
        var future2 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(172));
        var booking = await SeedHelper.SeedConfirmedBooking(db, auth.Id, seed.Hotel,
            seed.HotelRoomType, seed.Rooms[0], future, future2);

        var response = await client.GetAsync($"/api/v1/bookings/{booking.Id}");
        var details = await response.ReadJsonAsync<BookingDetailsResponse>();

        details.Should().NotBeNull();
        details!.BookingNumber.Should().NotBeNullOrEmpty();
        details.HotelName.Should().NotBeNullOrEmpty();
        details.HotelAddress.Should().NotBeNullOrEmpty();
        details.CheckIn.Should().Be(future);
        details.CheckOut.Should().Be(future2);
        details.TotalAmount.Should().BeGreaterThan(0);
        details.Rooms.Should().NotBeEmpty();
    }

    [Fact]
    public async Task FullFlow_AfterBooking_RoomAvailabilityDecreases()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(180));
        var future2 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(181));

        // Check initial availability
        var client = _factory.CreateClient();
        var beforeResp = await client.GetAsync(
            $"/api/v1/hotels/{seed.Hotel.Id}/room-availability?checkIn={future:yyyy-MM-dd}&checkOut={future2:yyyy-MM-dd}");
        var before = await beforeResp.ReadJsonAsync<RoomAvailabilityResponse>();
        var availBefore = before!.RoomTypes.First().AvailableRooms;

        // Book one room
        await SeedHelper.SeedConfirmedBooking(db, Guid.NewGuid(), seed.Hotel,
            seed.HotelRoomType, seed.Rooms[0], future, future2);

        var afterResp = await client.GetAsync(
            $"/api/v1/hotels/{seed.Hotel.Id}/room-availability?checkIn={future:yyyy-MM-dd}&checkOut={future2:yyyy-MM-dd}");
        var after = await afterResp.ReadJsonAsync<RoomAvailabilityResponse>();
        var availAfter = after!.RoomTypes.First().AvailableRooms;

        availAfter.Should().BeLessThan(availBefore);
    }

    [Fact]
    public async Task FullFlow_AfterTrackView_HotelAppearsInRecentlyVisited()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client,
            $"trackview-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        await client.PostAsJsonAsync("/api/v1/events/hotel-viewed",
            new TrackHotelViewRequest(seed.Hotel.Id));

        var response = await client.GetAsync("/api/v1/events/recently-visited");
        var result = await response.ReadJsonAsync<HotelBooking.Contracts.Events.RecentlyVisitedResponse>();

        result.Should().NotBeNull();
        result!.Items.Should().Contain(x => x.HotelId == seed.Hotel.Id);
    }

    [Fact]
    public async Task FullFlow_BookAndCancel_RoomsBecomeFree()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client,
            $"bookcancel-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(190));
        var future2 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(191));

        var booking = await SeedHelper.SeedConfirmedBooking(db, auth.Id, seed.Hotel,
            seed.HotelRoomType, seed.Rooms[0], future, future2);

        // Cancel
        await client.PostAsJsonAsync($"/api/v1/bookings/{booking.Id}/cancel",
            new CancelBookingRequest("Free the rooms"));

        // Check availability
        var availResp = await client.GetAsync(
            $"/api/v1/hotels/{seed.Hotel.Id}/room-availability?checkIn={future:yyyy-MM-dd}&checkOut={future2:yyyy-MM-dd}");
        var avail = await availResp.ReadJsonAsync<RoomAvailabilityResponse>();

        // Room should be available (cancelled bookings don't count)
        avail!.RoomTypes.First().AvailableRooms.Should().Be(5);
    }
}
