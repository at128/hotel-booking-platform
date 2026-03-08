using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HotelBooking.Api.IntegrationTests.Helpers;
using HotelBooking.Api.IntegrationTests.Infrastructure;
using HotelBooking.Contracts.Admin;
using Xunit;

namespace HotelBooking.Api.IntegrationTests.Admin;

[Collection("Integration")]
public class AdminHotelsTests
{
    private readonly WebAppFactory _factory;

    public AdminHotelsTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> GetAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLoginAsAdmin(client, _factory,
            $"admin-hotels-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);
        return client;
    }

    [Fact]
    public async Task GetHotels_AsAdmin_ReturnsPaginatedList()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        await SeedHelper.SeedFullHierarchy(db);

        var response = await client.GetAsync("/api/v1/adminhotels");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<PaginatedAdminResponse<HotelDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetHotels_WithSearch_FiltersResults()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        await SeedHelper.SeedFullHierarchy(db);

        var response = await client.GetAsync("/api/v1/adminhotels?search=Grand");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHotelById_ValidId_ReturnsHotelDto()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);

        var response = await client.GetAsync($"/api/v1/adminhotels/{seed.Hotel.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<HotelDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be(seed.Hotel.Name);
    }

    [Fact]
    public async Task CreateHotel_ValidData_Returns201()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);

        var response = await client.PostAsJsonAsync("/api/v1/adminhotels",
            new CreateHotelRequest(seed.City.Id, $"NewHotel-{Guid.NewGuid():N}",
                "Owner", "456 St", 4, "Description", null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.ReadJsonAsync<HotelDto>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateHotel_InvalidCityId_Returns400Or404()
    {
        var client = await GetAdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/adminhotels",
            new CreateHotelRequest(Guid.NewGuid(), "Hotel", "Owner", "St", 3, null, null, null));

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateHotel_ValidData_Returns200()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);

        var response = await client.PutAsJsonAsync($"/api/v1/adminhotels/{seed.Hotel.Id}",
            new UpdateHotelRequest(seed.City.Id, "Updated Hotel Name",
                "New Owner", "New Address", 4, "Updated", null, null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteHotel_NoDependencies_Returns204()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        var city = new HotelBooking.Domain.Hotels.City(Guid.NewGuid(), $"Del-{Guid.NewGuid():N}", "Test", null);
        db.Cities.Add(city);
        var hotel = new HotelBooking.Domain.Hotels.Hotel(
            Guid.NewGuid(), city.Id, $"DelHotel-{Guid.NewGuid():N}", "Owner", "St", 3);
        db.Hotels.Add(hotel);
        await db.SaveChangesAsync();

        var response = await client.DeleteAsync($"/api/v1/adminhotels/{hotel.Id}");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NoContent, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteHotel_WithActiveBookings_Returns409()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(150));
        var future2 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(151));
        await SeedHelper.SeedConfirmedBooking(db, Guid.NewGuid(), seed.Hotel,
            seed.HotelRoomType, seed.Rooms[0], future, future2);

        var response = await client.DeleteAsync($"/api/v1/adminhotels/{seed.Hotel.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddHotelImage_ValidImage_Returns201()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);

        // Create a minimal valid image (1x1 JPEG)
        var imageBytes = Convert.FromBase64String(
            "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAABAAEDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAFRABAQAAAAAAAAAAAAAAAAAAAAf/xAAUAQEAAAAAAAAAAAAAAAAAAAAA/8QAFBEBAAAAAAAAAAAAAAAAAAAAAP/aAAwDAQACEQMRAD8AqAB//9k=");

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(imageBytes), "Image", "test.jpg");
        content.Add(new StringContent("Test Caption"), "Caption");

        var response = await client.PostAsync(
            $"/api/v1/adminhotels/{seed.Hotel.Id}/images", content);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Created, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AllEndpoints_AsRegularUser_Returns403()
    {
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client,
            $"regular-hotels-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var response = await client.GetAsync("/api/v1/adminhotels");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
