using FluentAssertions;
using HotelBooking.Application.Features.Auth.Commands.Login;

namespace HotelBooking.Application.UnitTests.Auth;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    [Trait("Category", "Unit")]
    public void ValidCommand_ShouldPass()
    {
        var command = new LoginCommand("test@email.com", "Secure123!");
        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void EmptyEmail_ShouldFail()
    {
        var command = new LoginCommand("", "Secure123!");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void InvalidEmail_ShouldFail()
    {
        var command = new LoginCommand("not-an-email", "Secure123!");
        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void EmptyPassword_ShouldFail()
    {
        var command = new LoginCommand("test@email.com", "");
        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}
