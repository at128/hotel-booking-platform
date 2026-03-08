using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HotelBooking.Api.IntegrationTests.Helpers;
using HotelBooking.Api.IntegrationTests.Infrastructure;
using HotelBooking.Contracts.Admin;
using Xunit;

namespace HotelBooking.Api.IntegrationTests.Admin;

[Collection("Integration")]
public class AdminServicesTests
{
    private readonly WebAppFactory _factory;

    public AdminServicesTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> GetAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLoginAsAdmin(client, _factory,
            $"admin-svc-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);
        return client;
    }

    [Fact]
    public async Task GetServices_AsAdmin_ReturnsList()
    {
        var client = await GetAdminClientAsync();

        var response = await client.GetAsync("/api/v1/adminservices");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateService_ValidData_Returns201()
    {
        var client = await GetAdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/adminservices",
            new CreateServiceRequest($"Svc-{Guid.NewGuid():N}", "Service desc"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.ReadJsonAsync<ServiceDto>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateService_ValidData_Returns200()
    {
        var client = await GetAdminClientAsync();

        var createResp = await client.PostAsJsonAsync("/api/v1/adminservices",
            new CreateServiceRequest($"UpdSvc-{Guid.NewGuid():N}", null));
        var created = await createResp.ReadJsonAsync<ServiceDto>();

        var response = await client.PutAsJsonAsync($"/api/v1/adminservices/{created!.Id}",
            new UpdateServiceRequest("Updated Service", "Updated desc"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteService_Returns204()
    {
        var client = await GetAdminClientAsync();

        var createResp = await client.PostAsJsonAsync("/api/v1/adminservices",
            new CreateServiceRequest($"DelSvc-{Guid.NewGuid():N}", null));
        var created = await createResp.ReadJsonAsync<ServiceDto>();

        var response = await client.DeleteAsync($"/api/v1/adminservices/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AllEndpoints_AsRegularUser_Returns403()
    {
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client,
            $"regular-svc-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var response = await client.GetAsync("/api/v1/adminservices");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
