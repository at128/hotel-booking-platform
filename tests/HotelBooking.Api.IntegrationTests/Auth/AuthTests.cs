using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HotelBooking.Api.IntegrationTests.Helpers;
using HotelBooking.Api.IntegrationTests.Infrastructure;
using HotelBooking.Contracts.Auth;
using Xunit;

namespace HotelBooking.Api.IntegrationTests.Auth;

[Collection("Integration")]
public class AuthTests
{
    private readonly WebAppFactory _factory;
    private readonly HttpClient _client;

    public AuthTests(WebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsSuccessAndUserId()
    {
        var request = new RegisterRequest(
            $"register-valid-{Guid.NewGuid():N}@test.com",
            "Test@12345678", "John", "Doe", null);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.ReadJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.Email.Should().Be(request.Email);
        result.Token.Should().NotBeNull();
        result.Token.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var email = $"dup-{Guid.NewGuid():N}@test.com";
        var request = new RegisterRequest(email, "Test@12345678", "John", "Doe", null);

        await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns400()
    {
        var request = new RegisterRequest(
            $"weak-{Guid.NewGuid():N}@test.com",
            "123", "John", "Doe", null);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithMissingFields_Returns400()
    {
        var request = new RegisterRequest("", "", "", "", null);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAccessTokenAndRefreshToken()
    {
        var email = $"login-valid-{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "Test@12345678", "Test", "User", null));

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, "Test@12345678"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.Token.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var email = $"wrong-pw-{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "Test@12345678", "Test", "User", null));

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, "WrongPassword@123"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("nonexistent@test.com", "Test@12345678"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithValidRefreshToken_ReturnsNewTokens()
    {
        var email = $"refresh-{Guid.NewGuid():N}@test.com";
        var client = _factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "Test@12345678", "Test", "User", null));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, "Test@12345678"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var cookieHeader = BuildCookieHeader(loginResponse);
        cookieHeader.Should().NotBeNullOrWhiteSpace("login should return refresh-token cookies");

        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshRequest.Headers.Add("Cookie", cookieHeader!);

        var response = await client.SendAsync(refreshRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<TokenResponse>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_Returns401()
    {
        var client = _factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        request.Headers.Add("Cookie", "refreshToken=invalid-token");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_WithValidToken_ReturnsProfileResponse()
    {
        var email = $"profile-{Guid.NewGuid():N}@test.com";
        var auth = await AuthHelper.RegisterAndLogin(_client, email);
        var client = _factory.CreateClient();
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var response = await client.GetAsync("/api/v1/auth/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.ReadJsonAsync<ProfileResponse>();
        profile.Should().NotBeNull();
        profile!.Email.Should().Be(email);
    }

    [Fact]
    public async Task GetProfile_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_WithValidData_Returns200()
    {
        var email = $"update-{Guid.NewGuid():N}@test.com";
        var auth = await AuthHelper.RegisterAndLogin(_client, email);
        var client = _factory.CreateClient();
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var response = await client.PutAsJsonAsync("/api/v1/auth/profile",
            new UpdateProfileRequest("Updated", "Name", "+123456789"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.ReadJsonAsync<ProfileResponse>();
        profile.Should().NotBeNull();
        profile!.FirstName.Should().Be("Updated");
    }

    [Fact]
    public async Task ChangePassword_WithValidCurrentPassword_Returns204_AndAllowsLoginWithNewPassword()
    {
        var email = $"change-password-{Guid.NewGuid():N}@test.com";
        var oldPassword = "OldP@ssw0rd1";
        var newPassword = "NewP@ssw0rd2";

        var auth = await AuthHelper.RegisterAndLogin(_client, email, oldPassword);

        var authenticatedClient = _factory.CreateClient();
        AuthHelper.SetAuthToken(authenticatedClient, auth.Token.AccessToken);

        var response = await authenticatedClient.PostAsJsonAsync(
            "/api/v1/auth/change-password",
            new ChangePasswordRequest(oldPassword, newPassword));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var oldLoginResponse = await _factory.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, oldPassword));

        oldLoginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var newLoginResponse = await _factory.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, newPassword));

        newLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_Returns401()
    {
        var email = $"change-password-wrong-{Guid.NewGuid():N}@test.com";
        var oldPassword = "OldP@ssw0rd1";
        var newPassword = "NewP@ssw0rd2";

        var auth = await AuthHelper.RegisterAndLogin(_client, email, oldPassword);

        var authenticatedClient = _factory.CreateClient();
        AuthHelper.SetAuthToken(authenticatedClient, auth.Token.AccessToken);

        var response = await authenticatedClient.PostAsJsonAsync(
            "/api/v1/auth/change-password",
            new ChangePasswordRequest("WrongP@ssw0rd1", newPassword));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutCurrentSession_InvalidatesRefreshToken()
    {
        var email = $"logout-{Guid.NewGuid():N}@test.com";
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client, email);
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var logoutResponse = await client.PostAsync("/api/v1/auth/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refreshResponse = await client.PostAsync("/api/v1/auth/refresh", null);
        refreshResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutAllSessions_InvalidatesAllRefreshTokens()
    {
        var email = $"logout-all-{Guid.NewGuid():N}@test.com";
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client, email);
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var logoutResponse = await client.PostAsync("/api/v1/auth/logout-all", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refreshResponse = await client.PostAsync("/api/v1/auth/refresh", null);
        refreshResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    private static string? BuildCookieHeader(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            return null;

        var cookies = setCookies
            .Select(x => x.Split(';', 2)[0].Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        return cookies.Length == 0 ? null : string.Join("; ", cookies);
    }
}
