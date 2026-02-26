using Asp.Versioning;
using HotelBooking.Application.Features.Auth.Commands.Login;
using HotelBooking.Application.Features.Auth.Commands.RefreshToken;
using HotelBooking.Application.Features.Auth.Commands.Register;
using HotelBooking.Application.Features.Auth.Commands.LogoutCurrentSession;
using HotelBooking.Application.Features.Auth.Commands.UpdateProfile;
using HotelBooking.Application.Features.Auth.Queries.GetProfile;
using HotelBooking.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace HotelBooking.Api.Controllers;

public sealed class AuthController(ISender sender) : ApiController
{
    /// <summary>Register a new user account.</summary>
    [EnableRateLimiting("auth")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new RegisterCommand(
            request.Email, request.Password, request.FirstName,
            request.LastName, request.PhoneNumber), ct);

        return result.Match(
            response => CreatedAtAction(nameof(GetProfile), response),
            Problem);
    }

    /// <summary>Login and receive a JWT token.</summary>
    [EnableRateLimiting("auth")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new LoginCommand(request.Email, request.Password), ct);

        return result.Match(Ok, Problem);
    }

    /// <summary>Get the current user's profile.</summary>
    [Authorize]
    [HttpGet("profile")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await sender.Send(new GetProfileQuery(userId), ct);

        return result.Match(Ok, Problem);
    }

    /// <summary>Update the current user's profile.</summary>
    [Authorize]
    [HttpPut("profile")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await sender.Send(new UpdateProfileCommand(
            userId, request.FirstName, request.LastName, request.PhoneNumber), ct);

        return result.Match(Ok, Problem);
    }

    [HttpPost("refresh")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Refresh(
    [FromBody] RefreshTokenRequest request,
    CancellationToken ct)
    {
        var result = await sender.Send(
            new RefreshTokenCommand(request.RefreshToken),
            ct);

        return result.Match(Ok, Problem);
    }


    [HttpPost("logout")]
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Logout(
    [FromBody] RevokeTokenRequest request,
    CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Problem(title: "Unauthorized", statusCode: StatusCodes.Status401Unauthorized);

        var result = await sender.Send(
            new LogoutCurrentSessionCommand(
                userId,
                request.RefreshToken),
            ct);

        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("logout-all")]
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> LogoutAllSessions(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Problem(title: "Unauthorized", statusCode: StatusCodes.Status401Unauthorized);

        var result = await sender.Send(
            new HotelBooking.Application.Features.Auth.Commands.LogoutAllSessions.LogoutAllSessionsCommand(userId),
            ct);

        return result.Match(_ => NoContent(), Problem);
    }
}