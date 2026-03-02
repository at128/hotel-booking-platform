using FluentAssertions;
using HotelBooking.Application.Features.Auth.Commands.Register;

namespace HotelBooking.Application.UnitTests.Auth;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    [Trait("Category", "Unit")]
    public void ValidCommand_ShouldPass()
    {
        var command = new RegisterCommand("test@email.com", "Secure123!", "John", "Doe", null);
        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ValidCommand_WithPhoneNumber_ShouldPass()
    {
        var command = new RegisterCommand("test@email.com", "Secure123!", "John", "Doe", "+972591234567");
        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("short1!")]
    [InlineData("nouppercase1!")]
    [InlineData("NOLOWERCASE1!")]
    [InlineData("NoDigits!!")]
    [InlineData("NoSpecial123")]
    public void WeakPassword_ShouldFail(string password)
    {
        var command = new RegisterCommand("test@email.com", password, "John", "Doe", null);
        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void EmptyEmail_ShouldFail()
    {
        var command = new RegisterCommand("", "Secure123!", "John", "Doe", null);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void InvalidEmail_ShouldFail()
    {
        var command = new RegisterCommand("not-an-email", "Secure123!", "John", "Doe", null);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void EmptyFirstName_ShouldFail()
    {
        var command = new RegisterCommand("test@email.com", "Secure123!", "", "Doe", null);
        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void EmptyLastName_ShouldFail()
    {
        var command = new RegisterCommand("test@email.com", "Secure123!", "John", "", null);
        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void EmptyPassword_ShouldFail()
    {
        var command = new RegisterCommand("test@email.com", "", "John", "Doe", null);
        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TooLongPhoneNumber_ShouldFail()
    {
        var command = new RegisterCommand("test@email.com", "Secure123!", "John", "Doe",
            "123456789012345678901"); 
        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}
