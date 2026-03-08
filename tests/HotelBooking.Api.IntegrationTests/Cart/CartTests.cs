using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HotelBooking.Api.IntegrationTests.Helpers;
using HotelBooking.Api.IntegrationTests.Infrastructure;
using HotelBooking.Contracts.Cart;
using Xunit;

namespace HotelBooking.Api.IntegrationTests.Cart;

[Collection("Integration")]
public class CartTests
{
    private readonly WebAppFactory _factory;

    public CartTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, SeedResult Seed, string AccessToken)> SetupAsync()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client, $"cart-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);
        return (client, seed, auth.Token.AccessToken);
    }

    [Fact]
    public async Task AddToCart_ValidRoom_ReturnsCreatedCartItem()
    {
        var (client, seed, _) = await SetupAsync();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        var response = await client.PostAsJsonAsync("/api/v1/cart/items",
            new AddToCartRequest(seed.HotelRoomType.Id, tomorrow, dayAfter, 1, 2, 0));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var item = await response.ReadJsonAsync<CartItemDto>();
        item.Should().NotBeNull();
        item!.HotelRoomTypeId.Should().Be(seed.HotelRoomType.Id);
    }

    [Fact]
    public async Task AddToCart_DifferentHotel_ReturnsConflict()
    {
        var (client, seed, _) = await SetupAsync();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        // Add first item
        await client.PostAsJsonAsync("/api/v1/cart/items",
            new AddToCartRequest(seed.HotelRoomType.Id, tomorrow, dayAfter, 1, 2, 0));

        // Create a second hotel with room type
        using var db = _factory.CreateDbContext();
        var hotel2 = new HotelBooking.Domain.Hotels.Hotel(
            Guid.NewGuid(), seed.City.Id, "Other Hotel", "Owner", "456 St", 4);
        db.Hotels.Add(hotel2);
        var hrt2 = new HotelBooking.Domain.Hotels.HotelRoomType(
            Guid.NewGuid(), hotel2.Id, seed.RoomType.Id, 100m, 2, 0);
        db.HotelRoomTypes.Add(hrt2);
        db.Rooms.Add(new HotelBooking.Domain.Rooms.Room(
            Guid.NewGuid(), hrt2.Id, hotel2.Id, "201"));
        await db.SaveChangesAsync();

        // Try to add item from different hotel
        var response = await client.PostAsJsonAsync("/api/v1/cart/items",
            new AddToCartRequest(hrt2.Id, tomorrow, dayAfter, 1, 2, 0));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddToCart_DifferentDates_ReturnsConflict()
    {
        var (client, seed, _) = await SetupAsync();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
        var twoDaysAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));

        // Add first item with dates
        await client.PostAsJsonAsync("/api/v1/cart/items",
            new AddToCartRequest(seed.HotelRoomType.Id, tomorrow, dayAfter, 1, 2, 0));

        // Try adding with different dates (same hotel room type, different dates)
        // Need a second hotel room type to add to the same hotel
        using var db = _factory.CreateDbContext();
        var rt2 = new HotelBooking.Domain.Rooms.RoomType(
            Guid.NewGuid(),
            $"Standard-{Guid.NewGuid():N}",
            "Standard room");
        db.RoomTypes.Add(rt2);
        var hrt2 = new HotelBooking.Domain.Hotels.HotelRoomType(
            Guid.NewGuid(), seed.Hotel.Id, rt2.Id, 80m, 2, 0);
        db.HotelRoomTypes.Add(hrt2);
        db.Rooms.Add(new HotelBooking.Domain.Rooms.Room(
            Guid.NewGuid(), hrt2.Id, seed.Hotel.Id, "301"));
        await db.SaveChangesAsync();

        var response = await client.PostAsJsonAsync("/api/v1/cart/items",
            new AddToCartRequest(hrt2.Id, dayAfter, twoDaysAfter, 1, 2, 0));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddToCart_ExceedsCapacity_Returns400Or409()
    {
        var (client, seed, _) = await SetupAsync();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        // Try to add more guests than capacity allows (adult capacity is 2, child is 1)
        var response = await client.PostAsJsonAsync("/api/v1/cart/items",
            new AddToCartRequest(seed.HotelRoomType.Id, tomorrow, dayAfter, 1, 10, 5));

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddToCart_RoomNotAvailable_Returns409()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(40));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(41));

        // Book all rooms
        foreach (var room in seed.Rooms)
        {
            await SeedHelper.SeedConfirmedBooking(db, Guid.NewGuid(), seed.Hotel,
                seed.HotelRoomType, room, tomorrow, dayAfter);
        }

        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client, $"navail-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var response = await client.PostAsJsonAsync("/api/v1/cart/items",
            new AddToCartRequest(seed.HotelRoomType.Id, tomorrow, dayAfter, 5, 2, 0));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetCart_WithItems_ReturnsCartResponseWithPricing()
    {
        var (client, seed, _) = await SetupAsync();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        await client.PostAsJsonAsync("/api/v1/cart/items",
            new AddToCartRequest(seed.HotelRoomType.Id, tomorrow, dayAfter, 1, 2, 0));

        var response = await client.GetAsync("/api/v1/cart");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await response.ReadJsonAsync<CartResponse>();
        cart.Should().NotBeNull();
        cart!.Items.Should().NotBeEmpty();
        cart.Total.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetCart_Empty_ReturnsEmptyCartResponse()
    {
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client, $"empty-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var response = await client.GetAsync("/api/v1/cart");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await response.ReadJsonAsync<CartResponse>();
        cart.Should().NotBeNull();
        cart!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateCartItem_ChangeQuantity_Returns200()
    {
        var (client, seed, _) = await SetupAsync();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        var addResponse = await client.PostAsJsonAsync("/api/v1/cart/items",
            new AddToCartRequest(seed.HotelRoomType.Id, tomorrow, dayAfter, 1, 2, 0));
        var addedItem = await addResponse.ReadJsonAsync<CartItemDto>();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/cart/items/{addedItem!.Id}",
            new UpdateCartItemRequest(2));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.ReadJsonAsync<CartItemDto>();
        updated.Should().NotBeNull();
        updated!.Quantity.Should().Be(2);
    }

    [Fact]
    public async Task UpdateCartItem_OtherUsersItem_Returns403Or404()
    {
        var (client1, seed, _) = await SetupAsync();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        var addResponse = await client1.PostAsJsonAsync("/api/v1/cart/items",
            new AddToCartRequest(seed.HotelRoomType.Id, tomorrow, dayAfter, 1, 2, 0));
        var addedItem = await addResponse.ReadJsonAsync<CartItemDto>();

        // Second user
        var client2 = _factory.CreateClient();
        var auth2 = await AuthHelper.RegisterAndLogin(client2, $"other-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client2, auth2.Token.AccessToken);

        var response = await client2.PutAsJsonAsync(
            $"/api/v1/cart/items/{addedItem!.Id}",
            new UpdateCartItemRequest(3));

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveCartItem_ValidId_Returns204()
    {
        var (client, seed, _) = await SetupAsync();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        var addResponse = await client.PostAsJsonAsync("/api/v1/cart/items",
            new AddToCartRequest(seed.HotelRoomType.Id, tomorrow, dayAfter, 1, 2, 0));
        var addedItem = await addResponse.ReadJsonAsync<CartItemDto>();

        var response = await client.DeleteAsync($"/api/v1/cart/items/{addedItem!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ClearCart_RemovesAllItems_Returns204()
    {
        var (client, seed, _) = await SetupAsync();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        await client.PostAsJsonAsync("/api/v1/cart/items",
            new AddToCartRequest(seed.HotelRoomType.Id, tomorrow, dayAfter, 1, 2, 0));

        var response = await client.DeleteAsync("/api/v1/cart");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify cart is empty
        var cartResponse = await client.GetAsync("/api/v1/cart");
        var cart = await cartResponse.ReadJsonAsync<CartResponse>();
        cart!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task AllCartEndpoints_WithoutAuth_Return401()
    {
        var client = _factory.CreateClient();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        var getResponse = await client.GetAsync("/api/v1/cart");
        getResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var addResponse = await client.PostAsJsonAsync("/api/v1/cart/items",
            new AddToCartRequest(Guid.NewGuid(), tomorrow, dayAfter, 1, 2, 0));
        addResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var deleteResponse = await client.DeleteAsync("/api/v1/cart");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
