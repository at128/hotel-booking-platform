using System.Net.Http.Headers;
using System.Net.Http.Json;
using HotelBooking.Api.IntegrationTests.Infrastructure;
using HotelBooking.Contracts.Auth;
using HotelBooking.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotelBooking.Api.IntegrationTests.Helpers;

public static class AuthHelper
{
    public static async Task<AuthResponse> RegisterAndLogin(
        HttpClient client,
        string email = "testuser@test.com",
        string password = "Test@12345678",
        string firstName = "Test",
        string lastName = "User")
    {
        // Register
        await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, password, firstName, lastName, null));

        // Login
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();

        var result = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return result!;
    }

    public static async Task<AuthResponse> RegisterAndLoginAsAdmin(
        HttpClient client,
        WebAppFactory factory,
        string email = "admin@test.com",
        string password = "Admin@12345678")
    {
        // Register first
        await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, password, "Admin", "User", null));

        // Add Admin role directly via Identity
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        // Ensure Admin role exists
        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole<Guid> { Name = "Admin" });

        var user = await userManager.FindByEmailAsync(email);
        if (user is not null && !await userManager.IsInRoleAsync(user, "Admin"))
            await userManager.AddToRoleAsync(user, "Admin");

        // Re-login to get token with Admin claims
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();

        var result = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return result!;
    }

    public static void SetAuthToken(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
    }
}
