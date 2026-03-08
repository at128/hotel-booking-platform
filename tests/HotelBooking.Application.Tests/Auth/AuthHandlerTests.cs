/// <summary>
/// Tests for Auth command/query handlers: Register, Login, RefreshToken,
/// LogoutCurrentSession, LogoutAllSessions, UpdateProfile, GetProfile.
/// </summary>
using FluentAssertions;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;
using HotelBooking.Application.Features.Auth.Commands.ChangePassword;
using HotelBooking.Application.Features.Auth.Commands.Login;
using HotelBooking.Application.Features.Auth.Commands.LogoutAllSessions;
using HotelBooking.Application.Features.Auth.Commands.LogoutCurrentSession;
using HotelBooking.Application.Features.Auth.Commands.RefreshToken;
using HotelBooking.Application.Features.Auth.Commands.Register;
using HotelBooking.Application.Features.Auth.Commands.UpdateProfile;
using HotelBooking.Application.Features.Auth.Queries.GetProfile;
using HotelBooking.Application.Settings;
using HotelBooking.Contracts.Auth;
using HotelBooking.Domain.Common.Results;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
namespace HotelBooking.Application.Tests.Auth;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identity = new();
    private readonly Mock<ITokenProvider> _token = new();
    private readonly Mock<IRefreshTokenRepository> _rtRepo = new();
    private readonly Mock<ICookieService> _cookie = new();
    private readonly IOptions<RefreshTokenSettings> _rtOptions =
        Options.Create(new RefreshTokenSettings { ExpiryDays = 7, TokenBytes = 64 });

    private RegisterCommandHandler CreateHandler() =>
        new(_identity.Object, _token.Object, _rtRepo.Object, _cookie.Object, _rtOptions);

    private static RegisterCommand ValidCmd() =>
        new("user@test.com", "P@ssw0rd!", "John", "Doe", null);

    private static UserAuthResult FakeUser(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), "user@test.com", "John", "Doe",
            new List<string> { "User" }, DateTimeOffset.UtcNow);

    private static TokenResponse FakeToken() =>
        new("jwt_token_value", DateTime.UtcNow.AddMinutes(15));

    [Fact]
    public async Task Handle_Success_ReturnsAuthResponseWithToken()
    {
        // Arrange
        var user = FakeUser();
        _identity.Setup(x => x.RegisterUserAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), default))
            .ReturnsAsync(user);
        _token.Setup(x => x.GenerateJwtToken(It.IsAny<AppUserDto>()))
            .Returns(FakeToken());
        _token.Setup(x => x.GenerateRefreshToken()).Returns("raw_rt");
        _token.Setup(x => x.HashToken("raw_rt")).Returns("hashed_rt");

        // Act
        var result = await CreateHandler().Handle(ValidCmd(), default);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Email.Should().Be("user@test.com");
        result.Value.Token.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Success_CreatesRefreshTokenInRepository()
    {
        // Arrange
        var user = FakeUser();
        _identity.Setup(x => x.RegisterUserAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), default))
            .ReturnsAsync(user);
        _token.Setup(x => x.GenerateJwtToken(It.IsAny<AppUserDto>())).Returns(FakeToken());
        _token.Setup(x => x.GenerateRefreshToken()).Returns("raw_rt");
        _token.Setup(x => x.HashToken("raw_rt")).Returns("hashed_rt");

        // Act
        await CreateHandler().Handle(ValidCmd(), default);

        // Assert
        _rtRepo.Verify(x => x.AddAsync(It.IsAny<RefreshTokenData>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_Success_SetsCookieViaService()
    {
        // Arrange
        var user = FakeUser();
        _identity.Setup(x => x.RegisterUserAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), default))
            .ReturnsAsync(user);
        _token.Setup(x => x.GenerateJwtToken(It.IsAny<AppUserDto>())).Returns(FakeToken());
        _token.Setup(x => x.GenerateRefreshToken()).Returns("raw_rt");
        _token.Setup(x => x.HashToken("raw_rt")).Returns("hashed_rt");

        // Act
        await CreateHandler().Handle(ValidCmd(), default);

        // Assert
        _cookie.Verify(x => x.SetRefreshTokenCookie("raw_rt"), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailAlreadyExists_ReturnsError()
    {
        // Arrange
        _identity.Setup(x => x.RegisterUserAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), default))
            .ReturnsAsync(ApplicationErrors.Auth.EmailAlreadyRegistered);

        // Act
        var result = await CreateHandler().Handle(ValidCmd(), default);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(ApplicationErrors.Auth.EmailAlreadyRegistered.Code);
    }

    [Fact]
    public async Task Handle_TokenGenerationFails_ReturnsError()
    {
        // Arrange
        var user = FakeUser();
        _identity.Setup(x => x.RegisterUserAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), default))
            .ReturnsAsync(user);
        var tokenError = Error.Failure("Token.Failed", "Could not generate token");
        _token.Setup(x => x.GenerateJwtToken(It.IsAny<AppUserDto>()))
            .Returns(tokenError);

        // Act
        var result = await CreateHandler().Handle(ValidCmd(), default);

        // Assert
        result.IsError.Should().BeTrue();
    }
}

public class LoginCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identity = new();
    private readonly Mock<ITokenProvider> _token = new();
    private readonly Mock<IRefreshTokenRepository> _rtRepo = new();
    private readonly Mock<ICookieService> _cookie = new();
    private readonly IOptions<RefreshTokenSettings> _rtOptions =
        Options.Create(new RefreshTokenSettings { ExpiryDays = 7, TokenBytes = 64 });

    private LoginCommandHandler CreateHandler() =>
        new(_identity.Object, _token.Object, _rtRepo.Object, _cookie.Object, _rtOptions);

    private static UserAuthResult FakeUser() =>
        new(Guid.NewGuid(), "user@test.com", "John", "Doe",
            new List<string> { "User" }, DateTimeOffset.UtcNow);

    private static TokenResponse FakeToken() =>
        new("jwt_value", DateTime.UtcNow.AddMinutes(15));

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var user = FakeUser();
        _identity.Setup(x => x.ValidateCredentialsAsync("user@test.com", "P@ss", default))
            .ReturnsAsync(user);
        _token.Setup(x => x.GenerateJwtToken(It.IsAny<AppUserDto>())).Returns(FakeToken());
        _token.Setup(x => x.GenerateRefreshToken()).Returns("raw_rt");
        _token.Setup(x => x.HashToken("raw_rt")).Returns("hash_rt");

        // Act
        var result = await CreateHandler().Handle(new LoginCommand("user@test.com", "P@ss"), default);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Email.Should().Be("user@test.com");
    }

    [Fact]
    public async Task Handle_ValidCredentials_GeneratesAndSetsCookie()
    {
        // Arrange
        var user = FakeUser();
        _identity.Setup(x => x.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(user);
        _token.Setup(x => x.GenerateJwtToken(It.IsAny<AppUserDto>())).Returns(FakeToken());
        _token.Setup(x => x.GenerateRefreshToken()).Returns("raw_rt");
        _token.Setup(x => x.HashToken("raw_rt")).Returns("hash_rt");

        // Act
        await CreateHandler().Handle(new LoginCommand("user@test.com", "P@ss"), default);

        // Assert
        _rtRepo.Verify(x => x.AddAsync(It.IsAny<RefreshTokenData>(), default), Times.Once);
        _cookie.Verify(x => x.SetRefreshTokenCookie("raw_rt"), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidCredentials_ReturnsError()
    {
        // Arrange
        _identity.Setup(x => x.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(ApplicationErrors.Auth.InvalidCredentials);

        // Act
        var result = await CreateHandler().Handle(new LoginCommand("bad@test.com", "wrong"), default);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(ApplicationErrors.Auth.InvalidCredentials.Code);
    }

    [Fact]
    public async Task Handle_TokenGenerationFails_ReturnsError()
    {
        // Arrange
        var user = FakeUser();
        _identity.Setup(x => x.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(user);
        _token.Setup(x => x.GenerateJwtToken(It.IsAny<AppUserDto>()))
            .Returns(Error.Failure("T", "Failed"));

        // Act
        var result = await CreateHandler().Handle(new LoginCommand("u@t.com", "p"), default);

        // Assert
        result.IsError.Should().BeTrue();
    }
}

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<ITokenProvider> _token = new();
    private readonly Mock<IRefreshTokenRepository> _rtRepo = new();
    private readonly Mock<IIdentityService> _identity = new();
    private readonly Mock<ICookieService> _cookie = new();
    private readonly IOptions<RefreshTokenSettings> _rtOptions =
        Options.Create(new RefreshTokenSettings { ExpiryDays = 7, TokenBytes = 64 });

    private RefreshTokenCommandHandler CreateHandler() =>
        new(_token.Object, _rtRepo.Object, _identity.Object, _cookie.Object, _rtOptions);

    private static UserProfileResult FakeProfile(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), "user@test.com", "John", "Doe",
            null, "User", DateTimeOffset.UtcNow, null);

    private static RefreshTokenData FakeToken(Guid userId, bool isUsed = false, bool isActive = true) =>
        new(Guid.NewGuid(), userId, "hash", "family_123",
            IsActive: isActive, IsUsed: isUsed, IsRevoked: false, ExpiresAt: DateTimeOffset.UtcNow.AddDays(7));

    [Fact]
    public async Task Handle_NoCookie_ReturnsInvalidRefreshToken()
    {
        _cookie.Setup(x => x.GetRefreshTokenFromCookie()).Returns((string?)null);

        var result = await CreateHandler().Handle(new RefreshTokenCommand(), default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(ApplicationErrors.Auth.InvalidRefreshToken.Code);
    }

    [Fact]
    public async Task Handle_EmptyCookie_ReturnsInvalidRefreshToken()
    {
        _cookie.Setup(x => x.GetRefreshTokenFromCookie()).Returns("   ");

        var result = await CreateHandler().Handle(new RefreshTokenCommand(), default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(ApplicationErrors.Auth.InvalidRefreshToken.Code);
    }

    [Fact]
    public async Task Handle_TokenNotFound_ReturnsInvalidRefreshToken()
    {
        _cookie.Setup(x => x.GetRefreshTokenFromCookie()).Returns("raw_rt");
        _token.Setup(x => x.HashToken("raw_rt")).Returns("hash");
        _rtRepo.Setup(x => x.GetByHashAsync("hash", default)).ReturnsAsync((RefreshTokenData?)null);

        var result = await CreateHandler().Handle(new RefreshTokenCommand(), default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(ApplicationErrors.Auth.InvalidRefreshToken.Code);
    }

    [Fact]
    public async Task Handle_TokenAlreadyUsed_RevokesFamily_ReturnsReuse()
    {
        var userId = Guid.NewGuid();
        var storedToken = FakeToken(userId, isUsed: true);
        _cookie.Setup(x => x.GetRefreshTokenFromCookie()).Returns("raw_rt");
        _token.Setup(x => x.HashToken("raw_rt")).Returns("hash");
        _rtRepo.Setup(x => x.GetByHashAsync("hash", default)).ReturnsAsync(storedToken);

        var result = await CreateHandler().Handle(new RefreshTokenCommand(), default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(ApplicationErrors.Auth.RefreshTokenReuse.Code);
        _rtRepo.Verify(x => x.RevokeAllFamilyAsync("family_123", default), Times.Once);
    }

    [Fact]
    public async Task Handle_TokenNotActive_ReturnsInvalidRefreshToken()
    {
        var userId = Guid.NewGuid();
        var storedToken = FakeToken(userId, isActive: false);
        _cookie.Setup(x => x.GetRefreshTokenFromCookie()).Returns("raw_rt");
        _token.Setup(x => x.HashToken("raw_rt")).Returns("hash");
        _rtRepo.Setup(x => x.GetByHashAsync("hash", default)).ReturnsAsync(storedToken);

        var result = await CreateHandler().Handle(new RefreshTokenCommand(), default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(ApplicationErrors.Auth.InvalidRefreshToken.Code);
    }

    [Fact]
    public async Task Handle_ValidToken_RotatesAndReturnsNewJwt()
    {
        var userId = Guid.NewGuid();
        var storedToken = FakeToken(userId);
        var profile = FakeProfile(userId);

        _cookie.Setup(x => x.GetRefreshTokenFromCookie()).Returns("raw_rt");
        _token.Setup(x => x.HashToken("raw_rt")).Returns("hash");
        _token.Setup(x => x.HashToken("new_raw_rt")).Returns("new_hash");
        _rtRepo.Setup(x => x.GetByHashAsync("hash", default)).ReturnsAsync(storedToken);
        _identity.Setup(x => x.GetUserByIdAsync(userId, default)).ReturnsAsync(profile);
        _token.Setup(x => x.GenerateJwtToken(It.IsAny<AppUserDto>()))
            .Returns(new TokenResponse("new_jwt", DateTime.UtcNow.AddMinutes(15)));
        _token.Setup(x => x.GenerateRefreshToken()).Returns("new_raw_rt");
        _rtRepo.Setup(x => x.RotateAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<RefreshTokenData>(), It.IsAny<DateTimeOffset>(), default))
            .ReturnsAsync(true);

        var result = await CreateHandler().Handle(new RefreshTokenCommand(), default);

        result.IsError.Should().BeFalse();
        result.Value.AccessToken.Should().Be("new_jwt");
    }

    [Fact]
    public async Task Handle_RotationFails_RevokesFamily_ReturnsReuse()
    {
        var userId = Guid.NewGuid();
        var storedToken = FakeToken(userId);
        var profile = FakeProfile(userId);

        _cookie.Setup(x => x.GetRefreshTokenFromCookie()).Returns("raw_rt");
        _token.Setup(x => x.HashToken(It.IsAny<string>())).Returns("hash");
        _rtRepo.Setup(x => x.GetByHashAsync("hash", default)).ReturnsAsync(storedToken);
        _identity.Setup(x => x.GetUserByIdAsync(userId, default)).ReturnsAsync(profile);
        _token.Setup(x => x.GenerateJwtToken(It.IsAny<AppUserDto>()))
            .Returns(new TokenResponse("jwt", DateTime.UtcNow.AddMinutes(15)));
        _token.Setup(x => x.GenerateRefreshToken()).Returns("new_raw");
        _rtRepo.Setup(x => x.RotateAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<RefreshTokenData>(), It.IsAny<DateTimeOffset>(), default))
            .ReturnsAsync(false);

        var result = await CreateHandler().Handle(new RefreshTokenCommand(), default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(ApplicationErrors.Auth.RefreshTokenReuse.Code);
        _rtRepo.Verify(x => x.RevokeAllFamilyAsync("family_123", default), Times.Once);
    }
}

public class LogoutCurrentSessionCommandHandlerTests
{
    private readonly Mock<ITokenProvider> _token = new();
    private readonly Mock<IRefreshTokenRepository> _rtRepo = new();
    private readonly Mock<ICookieService> _cookie = new();

    private LogoutCurrentSessionCommandHandler CreateHandler() =>
        new(_token.Object, _rtRepo.Object, _cookie.Object);

    [Fact]
    public async Task Handle_WithValidCookie_RevokesFamilyAndRemovesCookie()
    {
        var userId = Guid.NewGuid();
        var storedToken = new RefreshTokenData(Guid.NewGuid(), userId, "hash", "fam",
            true, false, false, DateTimeOffset.UtcNow.AddDays(7));

        _cookie.Setup(x => x.GetRefreshTokenFromCookie()).Returns("raw_rt");
        _token.Setup(x => x.HashToken("raw_rt")).Returns("hash");
        _rtRepo.Setup(x => x.GetByHashAsync("hash", default)).ReturnsAsync(storedToken);

        var result = await CreateHandler().Handle(new LogoutCurrentSessionCommand(userId), default);

        result.IsError.Should().BeFalse();
        _rtRepo.Verify(x => x.RevokeAllFamilyAsync("fam", default), Times.Once);
        _cookie.Verify(x => x.RemoveRefreshTokenCookie(), Times.Once);
    }

    [Fact]
    public async Task Handle_NoCookie_JustRemovesCookie()
    {
        _cookie.Setup(x => x.GetRefreshTokenFromCookie()).Returns((string?)null);

        var result = await CreateHandler().Handle(new LogoutCurrentSessionCommand(Guid.NewGuid()), default);

        result.IsError.Should().BeFalse();
        _cookie.Verify(x => x.RemoveRefreshTokenCookie(), Times.Once);
        _rtRepo.Verify(x => x.RevokeAllFamilyAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_TokenNotFound_JustRemovesCookie()
    {
        _cookie.Setup(x => x.GetRefreshTokenFromCookie()).Returns("raw_rt");
        _token.Setup(x => x.HashToken("raw_rt")).Returns("hash");
        _rtRepo.Setup(x => x.GetByHashAsync("hash", default)).ReturnsAsync((RefreshTokenData?)null);

        var result = await CreateHandler().Handle(new LogoutCurrentSessionCommand(Guid.NewGuid()), default);

        result.IsError.Should().BeFalse();
        _rtRepo.Verify(x => x.RevokeAllFamilyAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_TokenBelongsToOtherUser_DoesNotRevoke()
    {
        var myUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var storedToken = new RefreshTokenData(Guid.NewGuid(), otherUserId, "hash", "fam",
            true, false, false, DateTimeOffset.UtcNow.AddDays(7));

        _cookie.Setup(x => x.GetRefreshTokenFromCookie()).Returns("raw_rt");
        _token.Setup(x => x.HashToken("raw_rt")).Returns("hash");
        _rtRepo.Setup(x => x.GetByHashAsync("hash", default)).ReturnsAsync(storedToken);

        await CreateHandler().Handle(new LogoutCurrentSessionCommand(myUserId), default);

        _rtRepo.Verify(x => x.RevokeAllFamilyAsync(It.IsAny<string>(), default), Times.Never);
    }
}

public class LogoutAllSessionsCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _rtRepo = new();
    private readonly Mock<ICookieService> _cookie = new();

    [Fact]
    public async Task Handle_RevokesAllForUserAndRemovesCookie()
    {
        var userId = Guid.NewGuid();
        var handler = new LogoutAllSessionsCommandHandler(_rtRepo.Object, _cookie.Object);

        var result = await handler.Handle(new LogoutAllSessionsCommand(userId), default);

        result.IsError.Should().BeFalse();
        _rtRepo.Verify(x => x.RevokeAllForUserAsync(userId, default), Times.Once);
        _cookie.Verify(x => x.RemoveRefreshTokenCookie(), Times.Once);
    }
}

public class UpdateProfileCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identity = new();

    [Fact]
    public async Task Handle_Success_ReturnsUpdatedProfile()
    {
        var userId = Guid.NewGuid();
        var profile = new UserProfileResult(userId, "u@t.com", "Jane", "Smith",
            "0501234567", "User", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        _identity.Setup(x => x.UpdateUserAsync("user-id", "Jane", "Smith", "0501234567", default))
            .ReturnsAsync(profile);

        var handler = new UpdateProfileCommandHandler(_identity.Object);
        var result = await handler.Handle(
            new UpdateProfileCommand("user-id", "Jane", "Smith", "0501234567"), default);

        result.IsError.Should().BeFalse();
        result.Value.FirstName.Should().Be("Jane");
        result.Value.LastName.Should().Be("Smith");
        result.Value.PhoneNumber.Should().Be("0501234567");
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsError()
    {
        _identity.Setup(x => x.UpdateUserAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string?>(), default))
            .ReturnsAsync(ApplicationErrors.Auth.UserNotFound);

        var handler = new UpdateProfileCommandHandler(_identity.Object);
        var result = await handler.Handle(
            new UpdateProfileCommand("bad-id", "X", "Y", null), default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(ApplicationErrors.Auth.UserNotFound.Code);
    }
}

public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identity = new();

    [Fact]
    public async Task Handle_Success_ReturnsSuccess()
    {
        _identity.Setup(x => x.ChangePasswordAsync(
                "user-id",
                "OldP@ssw0rd1",
                "NewP@ssw0rd2",
                default))
            .ReturnsAsync(Result.Success);

        var handler = new ChangePasswordCommandHandler(_identity.Object);

        var result = await handler.Handle(
            new ChangePasswordCommand("user-id", "OldP@ssw0rd1", "NewP@ssw0rd2"),
            default);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_InvalidCurrentPassword_ReturnsError()
    {
        _identity.Setup(x => x.ChangePasswordAsync(
                "user-id",
                "WrongP@ss1",
                "NewP@ssw0rd2",
                default))
            .ReturnsAsync(ApplicationErrors.Auth.InvalidCurrentPassword);

        var handler = new ChangePasswordCommandHandler(_identity.Object);

        var result = await handler.Handle(
            new ChangePasswordCommand("user-id", "WrongP@ss1", "NewP@ssw0rd2"),
            default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(ApplicationErrors.Auth.InvalidCurrentPassword.Code);
    }
}

public class GetProfileQueryHandlerTests
{
    private readonly Mock<IIdentityService> _identity = new();

    [Fact]
    public async Task Handle_Success_ReturnsProfile()
    {
        var userId = Guid.NewGuid();
        var profile = new UserProfileResult(userId, "u@t.com", "John", "Doe",
            null, "User", DateTimeOffset.UtcNow, null);
        _identity.Setup(x => x.GetUserByIdAsync(userId, default)).ReturnsAsync(profile);

        var handler = new GetProfileQueryHandler(_identity.Object);
        var result = await handler.Handle(new GetProfileQuery(userId), default);

        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(userId);
        result.Value.Email.Should().Be("u@t.com");
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsError()
    {
        var userId = Guid.NewGuid();
        _identity.Setup(x => x.GetUserByIdAsync(userId, default))
            .ReturnsAsync(ApplicationErrors.Auth.UserNotFound);

        var handler = new GetProfileQueryHandler(_identity.Object);
        var result = await handler.Handle(new GetProfileQuery(userId), default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(ApplicationErrors.Auth.UserNotFound.Code);
    }
}
