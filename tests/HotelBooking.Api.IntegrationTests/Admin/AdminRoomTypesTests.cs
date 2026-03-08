using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HotelBooking.Api.IntegrationTests.Helpers;
using HotelBooking.Api.IntegrationTests.Infrastructure;
using HotelBooking.Contracts.Admin;
using Xunit;

namespace HotelBooking.Api.IntegrationTests.Admin;

[Collection("Integration")]
public class AdminRoomTypesTests
{
    private readonly WebAppFactory _factory;

    public AdminRoomTypesTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> GetAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLoginAsAdmin(client, _factory,
            $"admin-rt-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);
        return client;
    }

    [Fact]
    public async Task GetRoomTypes_AsAdmin_ReturnsList()
    {
        var client = await GetAdminClientAsync();

        var response = await client.GetAsync("/api/v1/adminroomtypes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateRoomType_ValidData_Returns201()
    {
        var client = await GetAdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/adminroomtypes",
            new CreateRoomTypeRequest($"Suite-{Guid.NewGuid():N}", "Luxury suite"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.ReadJsonAsync<RoomTypeDto>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateRoomType_DuplicateName_Returns409()
    {
        var client = await GetAdminClientAsync();
        var name = $"DupRT-{Guid.NewGuid():N}";

        await client.PostAsJsonAsync("/api/v1/adminroomtypes",
            new CreateRoomTypeRequest(name, null));

        var response = await client.PostAsJsonAsync("/api/v1/adminroomtypes",
            new CreateRoomTypeRequest(name, null));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateRoomType_ValidData_Returns200()
    {
        var client = await GetAdminClientAsync();

        var createResp = await client.PostAsJsonAsync("/api/v1/adminroomtypes",
            new CreateRoomTypeRequest($"UpdRT-{Guid.NewGuid():N}", null));
        var created = await createResp.ReadJsonAsync<RoomTypeDto>();

        var response = await client.PutAsJsonAsync($"/api/v1/adminroomtypes/{created!.Id}",
            new UpdateRoomTypeRequest("Updated Name", "Updated desc"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteRoomType_NoDependencies_Returns204()
    {
        var client = await GetAdminClientAsync();

        var createResp = await client.PostAsJsonAsync("/api/v1/adminroomtypes",
            new CreateRoomTypeRequest($"DelRT-{Guid.NewGuid():N}", null));
        var created = await createResp.ReadJsonAsync<RoomTypeDto>();

        var response = await client.DeleteAsync($"/api/v1/adminroomtypes/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AllEndpoints_AsRegularUser_Returns403()
    {
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client,
            $"regular-rt-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var response = await client.GetAsync("/api/v1/adminroomtypes");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
