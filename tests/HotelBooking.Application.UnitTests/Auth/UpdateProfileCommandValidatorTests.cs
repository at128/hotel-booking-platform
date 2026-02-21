using FluentAssertions;
using HotelBooking.Application.Features.Auth.Commands.UpdateProfile;

namespace HotelBooking.Application.UnitTests.Auth;

public class UpdateProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _validator = new();

    [Fact]
    [Trait("Category", "Unit")]
    public void ValidCommand_ShouldPass()
    {
        var command = new UpdateProfileCommand("some-user-id", "John", "Doe", "+972591234567");
        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void NullPhoneNumber_ShouldPass()
    {
        var command = new UpdateProfileCommand("some-user-id", "John", "Doe", null);
        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void EmptyUserId_ShouldFail()
    {
        var command = new UpdateProfileCommand("", "John", "Doe", null);
        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void EmptyFirstName_ShouldFail()
    {
        var command = new UpdateProfileCommand("some-user-id", "", "Doe", null);
        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void EmptyLastName_ShouldFail()
    {
        var command = new UpdateProfileCommand("some-user-id", "John", "", null);
        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}
