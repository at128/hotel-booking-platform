/// <summary>
/// Tests for RegisterCommandValidator — all field constraints.
/// </summary>
using FluentValidation.TestHelper;
using HotelBooking.Application.Features.Auth.Commands.ChangePassword;
using HotelBooking.Application.Features.Auth.Commands.Register;
using HotelBooking.Application.Features.Auth.Commands.Login;
using HotelBooking.Application.Features.Auth.Commands.UpdateProfile;
using Xunit;
namespace HotelBooking.Application.Tests.Auth;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    private static RegisterCommand ValidCommand() => new(
        Email: "user@example.com",
        Password: "P@ssw0rd!",
        FirstName: "John",
        LastName: "Doe",
        PhoneNumber: null);

    [Fact]
    public void Valid_AllFieldsCorrect_NoErrors()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Email_Empty_Error()
    {
        var result = _validator.TestValidate(ValidCommand() with { Email = "" });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Email_InvalidFormat_Error()
    {
        var result = _validator.TestValidate(ValidCommand() with { Email = "not-an-email" });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Email_TooLong_Error()
    {
        var longEmail = new string('a', 248) + "@x.co"; // > 256
        var result = _validator.TestValidate(ValidCommand() with { Email = longEmail });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Password_Empty_Error()
    {
        var result = _validator.TestValidate(ValidCommand() with { Password = "" });
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Password_TooShort_Error()
    {
        var result = _validator.TestValidate(ValidCommand() with { Password = "P@1a" });
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Password_NoUppercase_Error()
    {
        var result = _validator.TestValidate(ValidCommand() with { Password = "p@ssw0rd" });
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Password_NoLowercase_Error()
    {
        var result = _validator.TestValidate(ValidCommand() with { Password = "P@SSW0RD" });
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Password_NoDigit_Error()
    {
        var result = _validator.TestValidate(ValidCommand() with { Password = "P@ssword" });
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Password_NoSpecialChar_Error()
    {
        var result = _validator.TestValidate(ValidCommand() with { Password = "Passw0rd" });
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void FirstName_Empty_Error()
    {
        var result = _validator.TestValidate(ValidCommand() with { FirstName = "" });
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void FirstName_TooLong_Error()
    {
        var result = _validator.TestValidate(ValidCommand() with { FirstName = new string('a', 101) });
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void LastName_Empty_Error()
    {
        var result = _validator.TestValidate(ValidCommand() with { LastName = "" });
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void LastName_TooLong_Error()
    {
        var result = _validator.TestValidate(ValidCommand() with { LastName = new string('z', 101) });
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void PhoneNumber_TooLong_Error()
    {
        var result = _validator.TestValidate(ValidCommand() with { PhoneNumber = new string('9', 21) });
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void PhoneNumber_Null_Valid()
    {
        var result = _validator.TestValidate(ValidCommand() with { PhoneNumber = null });
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }
}


/// <summary>
/// Tests for LoginCommandValidator.
/// </summary>
public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    private static LoginCommand Valid() => new("user@example.com", "P@ssw0rd!");

    [Fact]
    public void Valid_NoErrors()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Email_Empty_Error()
    {
        _validator.TestValidate(Valid() with { Email = "" }).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Email_InvalidFormat_Error()
    {
        _validator.TestValidate(Valid() with { Email = "bad" }).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Password_Empty_Error()
    {
        _validator.TestValidate(Valid() with { Password = "" }).ShouldHaveValidationErrorFor(x => x.Password);
    }
}


/// <summary>
/// Tests for UpdateProfileCommandValidator.
/// </summary>
public class UpdateProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _validator = new();

    private static UpdateProfileCommand Valid() =>
        new("user-id-123", "Jane", "Smith", null);

    [Fact]
    public void Valid_NoErrors()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UserId_Empty_Error()
    {
        _validator.TestValidate(Valid() with { UserId = "" }).ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void FirstName_Empty_Error()
    {
        _validator.TestValidate(Valid() with { FirstName = "" }).ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void LastName_Empty_Error()
    {
        _validator.TestValidate(Valid() with { LastName = "" }).ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void PhoneNumber_TooLong_Error()
    {
        _validator.TestValidate(Valid() with { PhoneNumber = new string('5', 21) })
            .ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }
}

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    private static ChangePasswordCommand Valid() =>
        new("user-id", "OldP@ssw0rd1", "NewP@ssw0rd2");

    [Fact]
    public void Valid_NoErrors()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UserId_Empty_Error()
    {
        _validator.TestValidate(Valid() with { UserId = "" })
            .ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void CurrentPassword_Empty_Error()
    {
        _validator.TestValidate(Valid() with { CurrentPassword = "" })
            .ShouldHaveValidationErrorFor(x => x.CurrentPassword);
    }

    [Fact]
    public void NewPassword_Empty_Error()
    {
        _validator.TestValidate(Valid() with { NewPassword = "" })
            .ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void NewPassword_TooShort_Error()
    {
        _validator.TestValidate(Valid() with { NewPassword = "P@1a" })
            .ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void NewPassword_SameAsCurrent_Error()
    {
        _validator.TestValidate(Valid() with { NewPassword = "OldP@ssw0rd1" })
            .ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}
