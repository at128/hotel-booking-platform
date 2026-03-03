using FluentAssertions;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;
using HotelBooking.Application.Features.Auth.Commands.RefreshToken;
using HotelBooking.Application.Settings;
using HotelBooking.Contracts.Auth;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace HotelBooking.Application.UnitTests.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly ITokenProvider _tokenProvider = Substitute.For<ITokenProvider>();
    private readonly IRefreshTokenRepository _repository = Substitute.For<IRefreshTokenRepository>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly ICookieService _cookieService = Substitute.For<ICookieService>();

    private RefreshTokenCommandHandler CreateHandler() => new(
        _tokenProvider,
        _repository,
        _identityService,
        _cookieService,
        Options.Create(new RefreshTokenSettings { ExpiryDays = 7, TokenBytes = 64 }));

    [Fact]
    public async Task Handle_WhenNoCookie_ReturnsInvalidToken()
    {
        _cookieService.GetRefreshTokenFromCookie().Returns((string?)null);
        var result = await CreateHandler().Handle(new RefreshTokenCommand(), default);
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(ApplicationErrors.Auth.InvalidRefreshToken.Code);
    }

    [Fact]
    public async Task Handle_WhenTokenAlreadyUsed_RevokesFamily_ReturnsReuse()
    {
        const string rawToken = "raw-token";
        const string hash = "hashed";
        const string family = "family-123";

        _cookieService.GetRefreshTokenFromCookie().Returns(rawToken);
        _tokenProvider.HashToken(rawToken).Returns(hash);
        _repository.GetByHashAsync(hash, default).Returns(
            new RefreshTokenData(
                Guid.NewGuid(), Guid.NewGuid(), hash, family,
                IsActive: false, IsUsed: true, IsRevoked: false,
                ExpiresAt: DateTimeOffset.UtcNow.AddDays(7)));

        var result = await CreateHandler().Handle(new RefreshTokenCommand(), default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(ApplicationErrors.Auth.RefreshTokenReuse.Code);
        await _repository.Received(1).RevokeAllFamilyAsync(family, default);
        _cookieService.Received(1).RemoveRefreshTokenCookie();
    }

    [Fact]
    public async Task Handle_WhenRotationFails_RevokesFamily_ReturnsReuse()
    {
        // Simulates concurrent request using same token
        const string rawToken = "raw-token";
        const string hash = "hashed";
        const string family = "family-123";
        var userId = Guid.NewGuid();

        _cookieService.GetRefreshTokenFromCookie().Returns(rawToken);
        _tokenProvider.HashToken(Arg.Any<string>()).Returns(hash);
        _repository.GetByHashAsync(hash, default).Returns(
            new RefreshTokenData(Guid.NewGuid(), userId, hash, family,
                IsActive: true, IsUsed: false, IsRevoked: false,
                ExpiresAt: DateTimeOffset.UtcNow.AddDays(7)));

        _identityService.GetUserByIdAsync(userId, default).Returns(
            new UserProfileResult(userId, "test@test.com", "John", "Doe",
                null, "User", DateTimeOffset.UtcNow, null));

        _tokenProvider.GenerateRefreshToken().Returns("new-raw-token");
        _tokenProvider.GenerateJwtToken(Arg.Any<AppUserDto>()).Returns(
            new TokenResponse("new-jwt", DateTime.UtcNow.AddMinutes(15)));

        // RotateAsync returns false = concurrent usage detected
        _repository.RotateAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<RefreshTokenData>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateHandler().Handle(new RefreshTokenCommand(), default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(ApplicationErrors.Auth.RefreshTokenReuse.Code);
        await _repository.Received(1).RevokeAllFamilyAsync(family, default);
    }

    [Fact]
    public async Task Handle_ValidToken_RotatesSuccessfully_ReturnsNewJwt()
    {
        const string rawToken = "raw-token";
        const string hash = "hashed";
        const string newHash = "new-hashed";
        const string family = "family-123";
        var userId = Guid.NewGuid();
        var expectedJwt = new TokenResponse("jwt-token", DateTime.UtcNow.AddMinutes(15));

        _cookieService.GetRefreshTokenFromCookie().Returns(rawToken);
        _tokenProvider.HashToken(rawToken).Returns(hash);
        _tokenProvider.HashToken("new-raw-token").Returns(newHash);
        _tokenProvider.GenerateRefreshToken().Returns("new-raw-token");
        _tokenProvider.GenerateJwtToken(Arg.Any<AppUserDto>()).Returns(expectedJwt);

        _repository.GetByHashAsync(hash, default).Returns(
            new RefreshTokenData(Guid.NewGuid(), userId, hash, family,
                IsActive: true, IsUsed: false, IsRevoked: false,
                ExpiresAt: DateTimeOffset.UtcNow.AddDays(7)));

        _identityService.GetUserByIdAsync(userId, default).Returns(
            new UserProfileResult(userId, "test@test.com", "John", "Doe",
                null, "User", DateTimeOffset.UtcNow, null));

        _repository.RotateAsync(
            Arg.Any<Guid>(), family, Arg.Any<RefreshTokenData>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await CreateHandler().Handle(new RefreshTokenCommand(), default);

        result.IsError.Should().BeFalse();
        result.Value.AccessToken.Should().Be("jwt-token");
        _cookieService.Received(1).SetRefreshTokenCookie("new-raw-token");
    }
}