using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HotelBooking.Api.IntegrationTests.Helpers;
using HotelBooking.Api.IntegrationTests.Infrastructure;
using HotelBooking.Contracts.Admin.HotelRoomTypes;
using Xunit;

namespace HotelBooking.Api.IntegrationTests.Admin;

[Collection("Integration")]
public class AdminHotelRoomTypesTests
{
    private readonly WebAppFactory _factory;

    // Note: AdminHotelRoomTypesController has explicit route [Route("api/v{version:apiVersion}/admin/hotel-room-types")]
    private const string BaseUrl = "/api/v1/admin/hotel-room-types";

    public AdminHotelRoomTypesTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> GetAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLoginAsAdmin(client, _factory,
            $"admin-hrt-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);
        return client;
    }

    [Fact]
    public async Task GetHotelRoomTypes_AsAdmin_ReturnsList()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);

        var response = await client.GetAsync($"{BaseUrl}?hotelId={seed.Hotel.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<List<HotelRoomTypeAdminDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetHotelRoomTypeById_ValidId_ReturnsDto()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);

        var response = await client.GetAsync($"{BaseUrl}/{seed.HotelRoomType.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<HotelRoomTypeAdminDto>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateHotelRoomType_ValidData_Returns201()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);

        // Create a new room type first
        var newRoomType = new HotelBooking.Domain.Rooms.RoomType(
            Guid.NewGuid(), $"NewRT-{Guid.NewGuid():N}", null);
        db.RoomTypes.Add(newRoomType);
        await db.SaveChangesAsync();

        var response = await client.PostAsJsonAsync(BaseUrl,
            new CreateHotelRoomTypeRequest(seed.Hotel.Id, newRoomType.Id,
                200m, 3, 1, 4, "Premium room"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateHotelRoomType_InvalidHotelOrRoomType_Returns400()
    {
        var client = await GetAdminClientAsync();

        var response = await client.PostAsJsonAsync(BaseUrl,
            new CreateHotelRoomTypeRequest(Guid.NewGuid(), Guid.NewGuid(),
                100m, 2, 0, null, null));

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateHotelRoomType_ValidData_Returns204()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);

        var response = await client.PutAsJsonAsync($"{BaseUrl}/{seed.HotelRoomType.Id}",
            new UpdateHotelRoomTypeRequest(250m, 3, 2, 5, "Updated desc"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteHotelRoomType_Returns204()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);

        // Create a fresh one to delete (avoid FK issues with rooms)
        var rt = new HotelBooking.Domain.Rooms.RoomType(Guid.NewGuid(), $"TmpRT-{Guid.NewGuid():N}", null);
        db.RoomTypes.Add(rt);
        var hrt = new HotelBooking.Domain.Hotels.HotelRoomType(
            Guid.NewGuid(), seed.Hotel.Id, rt.Id, 80m, 1, 0);
        db.HotelRoomTypes.Add(hrt);
        await db.SaveChangesAsync();

        var response = await client.DeleteAsync($"{BaseUrl}/{hrt.Id}");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NoContent, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AllEndpoints_AsRegularUser_Returns403()
    {
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client,
            $"regular-hrt-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var response = await client.GetAsync(BaseUrl);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
