using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HotelBooking.Api.IntegrationTests.Helpers;
using HotelBooking.Api.IntegrationTests.Infrastructure;
using HotelBooking.Contracts.Cart;
using HotelBooking.Contracts.Checkout;
using Xunit;

namespace HotelBooking.Api.IntegrationTests.Checkout;

[Collection("Integration")]
public class CheckoutTests
{
    private readonly WebAppFactory _factory;

    public CheckoutTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, SeedResult Seed, CheckoutHoldResponse Hold)> SetupWithHoldAsync()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client, $"checkout-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        // Add to cart
        await client.PostAsJsonAsync("/api/v1/cart/items",
            new AddToCartRequest(seed.HotelRoomType.Id, tomorrow, dayAfter, 1, 2, 0));

        // Create hold
        var holdResponse = await client.PostAsJsonAsync("/api/v1/checkout/hold",
            new CreateHoldRequest(null));
        holdResponse.EnsureSuccessStatusCode();
        var hold = await holdResponse.ReadJsonAsync<CheckoutHoldResponse>();

        return (client, seed, hold!);
    }

    [Fact]
    public async Task CreateCheckoutHold_WithCartItems_ReturnsHoldResponse()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client, $"hold-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        await client.PostAsJsonAsync("/api/v1/cart/items",
            new AddToCartRequest(seed.HotelRoomType.Id, tomorrow, dayAfter, 1, 2, 0));

        var response = await client.PostAsJsonAsync("/api/v1/checkout/hold",
            new CreateHoldRequest(null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var hold = await response.ReadJsonAsync<CheckoutHoldResponse>();
        hold.Should().NotBeNull();
        hold!.HoldIds.Should().NotBeEmpty();
        hold.Total.Should().BeGreaterThan(0);
        hold.ExpiresAtUtc.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task CreateCheckoutHold_EmptyCart_Returns400()
    {
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client, $"empty-hold-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var response = await client.PostAsJsonAsync("/api/v1/checkout/hold",
            new CreateHoldRequest(null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCheckoutHold_RoomBecameUnavailable_Returns409()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client, $"unavail-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(50));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(51));

        // Add all rooms to cart
        await client.PostAsJsonAsync("/api/v1/cart/items",
            new AddToCartRequest(seed.HotelRoomType.Id, tomorrow, dayAfter, 5, 2, 0));

        // Book all rooms by another user
        foreach (var room in seed.Rooms)
        {
            await SeedHelper.SeedConfirmedBooking(db, Guid.NewGuid(), seed.Hotel,
                seed.HotelRoomType, room, tomorrow, dayAfter);
        }

        var response = await client.PostAsJsonAsync("/api/v1/checkout/hold",
            new CreateHoldRequest(null));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateBooking_WithValidHold_ReturnsPendingBooking()
    {
        var (client, seed, hold) = await SetupWithHoldAsync();

        var response = await client.PostAsJsonAsync("/api/v1/checkout/booking",
            new CreateBookingRequest(hold.HoldIds, null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var booking = await response.ReadJsonAsync<CreateBookingResponse>();
        booking.Should().NotBeNull();
        booking!.BookingId.Should().NotBeEmpty();
        booking.BookingNumber.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateBooking_WithStripeSessionUrl_ReturnsCreateBookingResponse()
    {
        var (client, seed, hold) = await SetupWithHoldAsync();

        var response = await client.PostAsJsonAsync("/api/v1/checkout/booking",
            new CreateBookingRequest(hold.HoldIds, "Test booking"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var booking = await response.ReadJsonAsync<CreateBookingResponse>();
        booking.Should().NotBeNull();
        booking!.PaymentUrl.Should().NotBeNullOrEmpty();
        booking.PaymentUrl.Should().Contain("fake-stripe.com");
    }

    [Fact]
    public async Task CreateBooking_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/checkout/booking",
            new CreateBookingRequest(new List<Guid> { Guid.NewGuid() }, null));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
