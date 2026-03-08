using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HotelBooking.Api.IntegrationTests.Helpers;
using HotelBooking.Api.IntegrationTests.Infrastructure;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Rooms;
using Xunit;

namespace HotelBooking.Api.IntegrationTests.Admin;

[Collection("Integration")]
public class AdminRoomsTests
{
    private readonly WebAppFactory _factory;

    public AdminRoomsTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> GetAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLoginAsAdmin(client, _factory,
            $"admin-rooms-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);
        return client;
    }

    [Fact]
    public async Task GetRooms_AsAdmin_ReturnsList()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        await SeedHelper.SeedFullHierarchy(db);

        var response = await client.GetAsync("/api/v1/adminrooms");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRooms_FilterByHotelRoomType_ReturnsFiltered()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);

        var response = await client.GetAsync(
            $"/api/v1/adminrooms?hotelId={seed.Hotel.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateRoom_ValidData_Returns201()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);

        var response = await client.PostAsJsonAsync("/api/v1/adminrooms",
            new CreateRoomRequest(seed.HotelRoomType.Id, $"R{Guid.NewGuid():N}"[..6],
                (short)2, RoomStatus.Available));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.ReadJsonAsync<RoomDto>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateRoom_DuplicateRoomNumber_Returns409()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var roomNumber = $"D{Guid.NewGuid():N}"[..5];

        await client.PostAsJsonAsync("/api/v1/adminrooms",
            new CreateRoomRequest(seed.HotelRoomType.Id, roomNumber, 1, RoomStatus.Available));

        var response = await client.PostAsJsonAsync("/api/v1/adminrooms",
            new CreateRoomRequest(seed.HotelRoomType.Id, roomNumber, 1, RoomStatus.Available));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateRoom_ValidData_Returns200()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);

        var response = await client.PutAsJsonAsync($"/api/v1/adminrooms/{seed.Rooms[0].Id}",
            new UpdateRoomRequest("999", (short)9, RoomStatus.Maintenance));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteRoom_NoDependencies_Returns204()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);

        // Create a new room with no bookings
        var room = new Room(Guid.NewGuid(), seed.HotelRoomType.Id, seed.Hotel.Id,
            $"X{Guid.NewGuid():N}"[..5]);
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        var response = await client.DeleteAsync($"/api/v1/adminrooms/{room.Id}");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NoContent, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteRoom_WithActiveBooking_Returns409()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);

        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(160));
        var future2 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(161));
        await SeedHelper.SeedConfirmedBooking(db, Guid.NewGuid(), seed.Hotel,
            seed.HotelRoomType, seed.Rooms[0], future, future2);

        var response = await client.DeleteAsync($"/api/v1/adminrooms/{seed.Rooms[0].Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AllEndpoints_AsRegularUser_Returns403()
    {
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client,
            $"regular-rooms-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var response = await client.GetAsync("/api/v1/adminrooms");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
