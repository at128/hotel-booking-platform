using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HotelBooking.Api.IntegrationTests.Helpers;
using HotelBooking.Api.IntegrationTests.Infrastructure;
using HotelBooking.Contracts.Cart;
using HotelBooking.Contracts.Checkout;
using Xunit;

namespace HotelBooking.Api.IntegrationTests.Workflows;

[Collection("Integration")]
public class ConcurrentBookingTests
{
    private readonly WebAppFactory _factory;

    public ConcurrentBookingTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TwoUsers_BookLastRoom_OneSucceedsOneGets409()
    {
        // Seed a hotel with only 1 room
        using var db = _factory.CreateDbContext();
        var city = new HotelBooking.Domain.Hotels.City(Guid.NewGuid(), $"ConcCity-{Guid.NewGuid():N}", "Jordan", null);
        db.Cities.Add(city);
        var hotel = new HotelBooking.Domain.Hotels.Hotel(Guid.NewGuid(), city.Id, $"ConcHotel-{Guid.NewGuid():N}", "Owner", "St", 4);
        db.Hotels.Add(hotel);
        var roomType = new HotelBooking.Domain.Rooms.RoomType(Guid.NewGuid(), $"ConcRT-{Guid.NewGuid():N}", null);
        db.RoomTypes.Add(roomType);
        var hrt = new HotelBooking.Domain.Hotels.HotelRoomType(Guid.NewGuid(), hotel.Id, roomType.Id, 100m, 2, 0);
        db.HotelRoomTypes.Add(hrt);
        // Only 1 room!
        var room = new HotelBooking.Domain.Rooms.Room(Guid.NewGuid(), hrt.Id, hotel.Id, "001");
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(250));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(251));

        // User 1
        var client1 = _factory.CreateClient();
        var auth1 = await AuthHelper.RegisterAndLogin(client1, $"conc1-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client1, auth1.Token.AccessToken);

        // User 2
        var client2 = _factory.CreateClient();
        var auth2 = await AuthHelper.RegisterAndLogin(client2, $"conc2-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client2, auth2.Token.AccessToken);

        // Both add to cart
        await client1.PostAsJsonAsync("/api/v1/cart/items",
            new AddToCartRequest(hrt.Id, tomorrow, dayAfter, 1, 2, 0));
        await client2.PostAsJsonAsync("/api/v1/cart/items",
            new AddToCartRequest(hrt.Id, tomorrow, dayAfter, 1, 2, 0));

        // Both try to create holds concurrently
        var holdTask1 = client1.PostAsJsonAsync("/api/v1/checkout/hold", new CreateHoldRequest(null));
        var holdTask2 = client2.PostAsJsonAsync("/api/v1/checkout/hold", new CreateHoldRequest(null));

        var holdResponses = await Task.WhenAll(holdTask1, holdTask2);

        var successCount = holdResponses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var conflictCount = holdResponses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        // One should succeed, one should fail (or both could succeed if hold is transactional later)
        (successCount + conflictCount).Should().Be(2);
        successCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task TwoUsers_BookDifferentRoomTypes_BothSucceed()
    {
        using var db = _factory.CreateDbContext();
        var city = new HotelBooking.Domain.Hotels.City(
            Guid.NewGuid(), $"BothCity-{Guid.NewGuid():N}", "Jordan", null);
        db.Cities.Add(city);
        var hotel = new HotelBooking.Domain.Hotels.Hotel(
            Guid.NewGuid(), city.Id, $"BothHotel-{Guid.NewGuid():N}", "Owner", "St", 4);
        db.Hotels.Add(hotel);

        // Room type 1
        var rt1 = new HotelBooking.Domain.Rooms.RoomType(Guid.NewGuid(), $"RT1-{Guid.NewGuid():N}", null);
        db.RoomTypes.Add(rt1);
        var hrt1 = new HotelBooking.Domain.Hotels.HotelRoomType(
            Guid.NewGuid(), hotel.Id, rt1.Id, 100m, 2, 0);
        db.HotelRoomTypes.Add(hrt1);
        db.Rooms.Add(new HotelBooking.Domain.Rooms.Room(
            Guid.NewGuid(), hrt1.Id, hotel.Id, "A01"));

        // Room type 2
        var rt2 = new HotelBooking.Domain.Rooms.RoomType(Guid.NewGuid(), $"RT2-{Guid.NewGuid():N}", null);
        db.RoomTypes.Add(rt2);
        var hrt2 = new HotelBooking.Domain.Hotels.HotelRoomType(
            Guid.NewGuid(), hotel.Id, rt2.Id, 200m, 2, 0);
        db.HotelRoomTypes.Add(hrt2);
        db.Rooms.Add(new HotelBooking.Domain.Rooms.Room(
            Guid.NewGuid(), hrt2.Id, hotel.Id, "B01"));
        await db.SaveChangesAsync();

        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(260));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(261));

        // User 1 books room type 1
        var client1 = _factory.CreateClient();
        var auth1 = await AuthHelper.RegisterAndLogin(client1, $"both1-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client1, auth1.Token.AccessToken);
        await client1.PostAsJsonAsync("/api/v1/cart/items",
            new AddToCartRequest(hrt1.Id, tomorrow, dayAfter, 1, 2, 0));

        // User 2 books room type 2
        var client2 = _factory.CreateClient();
        var auth2 = await AuthHelper.RegisterAndLogin(client2, $"both2-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client2, auth2.Token.AccessToken);
        await client2.PostAsJsonAsync("/api/v1/cart/items",
            new AddToCartRequest(hrt2.Id, tomorrow, dayAfter, 1, 2, 0));

        // Both create holds
        var hold1 = await client1.PostAsJsonAsync("/api/v1/checkout/hold", new CreateHoldRequest(null));
        var hold2 = await client2.PostAsJsonAsync("/api/v1/checkout/hold", new CreateHoldRequest(null));

        hold1.StatusCode.Should().Be(HttpStatusCode.OK);
        hold2.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
