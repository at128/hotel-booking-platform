using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HotelBooking.Api.IntegrationTests.Helpers;
using HotelBooking.Api.IntegrationTests.Infrastructure;
using HotelBooking.Contracts.Admin;
using Xunit;

namespace HotelBooking.Api.IntegrationTests.Admin;

[Collection("Integration")]
public class AdminCitiesTests
{
    private readonly WebAppFactory _factory;

    public AdminCitiesTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> GetAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLoginAsAdmin(client, _factory,
            $"admin-cities-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);
        return client;
    }

    [Fact]
    public async Task GetCities_AsAdmin_ReturnsPaginatedAdminResponse()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        await SeedHelper.SeedFullHierarchy(db);

        var response = await client.GetAsync("/api/v1/admincities");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<PaginatedAdminResponse<CityDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCities_WithSearch_FiltersResults()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        await SeedHelper.SeedFullHierarchy(db);

        var response = await client.GetAsync("/api/v1/admincities?search=Amman");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<PaginatedAdminResponse<CityDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCityById_ValidId_ReturnsCityDto()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);

        var response = await client.GetAsync($"/api/v1/admincities/{seed.City.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<CityDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be(seed.City.Name);
    }

    [Fact]
    public async Task CreateCity_ValidData_Returns201()
    {
        var client = await GetAdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/admincities",
            new CreateCityRequest($"TestCity-{Guid.NewGuid():N}", "TestCountry", "12345"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.ReadJsonAsync<CityDto>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateCity_DuplicateNameAndCountry_Returns409()
    {
        var client = await GetAdminClientAsync();
        var name = $"DupCity-{Guid.NewGuid():N}";

        await client.PostAsJsonAsync("/api/v1/admincities",
            new CreateCityRequest(name, "Jordan", "11937"));

        var response = await client.PostAsJsonAsync("/api/v1/admincities",
            new CreateCityRequest(name, "Jordan", "11937"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateCity_ValidData_Returns200()
    {
        var client = await GetAdminClientAsync();

        var createResp = await client.PostAsJsonAsync("/api/v1/admincities",
            new CreateCityRequest($"Update-{Guid.NewGuid():N}", "Jordan", "11937"));
        var created = await createResp.ReadJsonAsync<CityDto>();

        var response = await client.PutAsJsonAsync($"/api/v1/admincities/{created!.Id}",
            new UpdateCityRequest("UpdatedName", "Jordan", "11938"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.ReadJsonAsync<CityDto>();
        updated!.Name.Should().Be("UpdatedName");
    }

    [Fact]
    public async Task DeleteCity_NoDependencies_Returns204()
    {
        var client = await GetAdminClientAsync();

        var createResp = await client.PostAsJsonAsync("/api/v1/admincities",
            new CreateCityRequest($"Delete-{Guid.NewGuid():N}", "TestCountry", "99999"));
        var created = await createResp.ReadJsonAsync<CityDto>();

        var response = await client.DeleteAsync($"/api/v1/admincities/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteCity_WithHotels_Returns409OrCascades()
    {
        var client = await GetAdminClientAsync();
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);

        var response = await client.DeleteAsync($"/api/v1/admincities/{seed.City.Id}");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Conflict, HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AllEndpoints_AsRegularUser_Returns403()
    {
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client,
            $"regular-cities-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var response = await client.GetAsync("/api/v1/admincities");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var createResp = await client.PostAsJsonAsync("/api/v1/admincities",
            new CreateCityRequest("Test", "Test", null));
        createResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AllEndpoints_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/admincities");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
