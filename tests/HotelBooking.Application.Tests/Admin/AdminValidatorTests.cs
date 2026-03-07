/// <summary>
/// Tests for Admin validator classes: CreateCity, CreateHotel, CreateRoom,
/// CreateService, CreateRoomType, and SearchHotels.
/// </summary>
using FluentValidation.TestHelper;
using HotelBooking.Application.Features.Admin.Cities.Command.CreateCity;
using HotelBooking.Application.Features.Admin.Cities.Command.UpdateCity;
using HotelBooking.Application.Features.Admin.Hotels.Command.CreateHotel;
using HotelBooking.Application.Features.Admin.RoomTypes.Commands.CreateRoomType;
using HotelBooking.Application.Features.Admin.Services.Commands.CreateService;
using Xunit;
namespace HotelBooking.Application.Tests.Admin.Cities;

/// <summary>Validator tests for CreateCityCommand.</summary>
public class CreateCityCommandValidatorTests
{
    private readonly CreateCityCommandValidator _v = new();

    private static CreateCityCommand Valid() => new("Amman", "Jordan", null);

    [Fact]
    public void Valid_NoErrors() => _v.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Name_Empty_Error()
        => _v.TestValidate(Valid() with { Name = "" }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Name_TooLong_Error()
        => _v.TestValidate(Valid() with { Name = new string('x', 101) }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Country_Empty_Error()
        => _v.TestValidate(Valid() with { Country = "" }).ShouldHaveValidationErrorFor(x => x.Country);

    [Fact]
    public void Country_TooLong_Error()
        => _v.TestValidate(Valid() with { Country = new string('x', 101) }).ShouldHaveValidationErrorFor(x => x.Country);

    [Fact]
    public void PostOffice_TooLong_Error()
        => _v.TestValidate(Valid() with { PostOffice = new string('0', 21) }).ShouldHaveValidationErrorFor(x => x.PostOffice);

    [Fact]
    public void PostOffice_Null_Valid()
        => _v.TestValidate(Valid() with { PostOffice = null }).ShouldNotHaveValidationErrorFor(x => x.PostOffice);
}


/// <summary>Validator tests for UpdateCityCommand.</summary>
public class UpdateCityCommandValidatorTests
{
    private readonly UpdateCityCommandValidator _v = new();

    private static UpdateCityCommand Valid() => new(Guid.NewGuid(), "Amman", "Jordan", null);

    [Fact]
    public void Valid_NoErrors() => _v.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Name_Empty_Error()
        => _v.TestValidate(Valid() with { Name = "" }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Country_Empty_Error()
        => _v.TestValidate(Valid() with { Country = "" }).ShouldHaveValidationErrorFor(x => x.Country);

    [Fact]
    public void PostOffice_TooLong_Error()
        => _v.TestValidate(Valid() with { PostOffice = new string('0', 21) }).ShouldHaveValidationErrorFor(x => x.PostOffice);
}


/// <summary>Validator tests for CreateHotelCommand.</summary>
public class CreateHotelCommandValidatorTests
{
    private readonly CreateHotelCommandValidator _v = new();

    private static CreateHotelCommand Valid() => new(
        CityId: Guid.NewGuid(),
        Name: "Grand Palace",
        Owner: "John Doe",
        Address: "1 King St",
        StarRating: 4,
        Description: null,
        Latitude: null,
        Longitude: null);

    [Fact]
    public void Valid_NoErrors() => _v.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Name_Empty_Error()
        => _v.TestValidate(Valid() with { Name = "" }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void CityId_Empty_Error()
        => _v.TestValidate(Valid() with { CityId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.CityId);

    [Fact]
    public void Owner_Empty_Error()
        => _v.TestValidate(Valid() with { Owner = "" }).ShouldHaveValidationErrorFor(x => x.Owner);

    [Fact]
    public void Address_Empty_Error()
        => _v.TestValidate(Valid() with { Address = "" }).ShouldHaveValidationErrorFor(x => x.Address);

    [Fact]
    public void StarRating_Zero_Error()
        => _v.TestValidate(Valid() with { StarRating = 0 }).ShouldHaveValidationErrorFor(x => x.StarRating);

    [Fact]
    public void StarRating_SixOrMore_Error()
        => _v.TestValidate(Valid() with { StarRating = 6 }).ShouldHaveValidationErrorFor(x => x.StarRating);

    [Fact]
    public void Description_TooLong_Error()
        => _v.TestValidate(Valid() with { Description = new string('x', 2001) }).ShouldHaveValidationErrorFor(x => x.Description);
}


/// <summary>Validator tests for CreateRoomTypeCommand.</summary>
public class CreateRoomTypeCommandValidatorTests
{
    private readonly CreateRoomTypeCommandValidator _v = new();

    private static CreateRoomTypeCommand Valid() => new("Deluxe", null);

    [Fact]
    public void Valid_NoErrors() => _v.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Name_Empty_Error()
        => _v.TestValidate(Valid() with { Name = "" }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Name_TooLong_Error()
        => _v.TestValidate(Valid() with { Name = new string('x', 101) }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Description_TooLong_Error()
        => _v.TestValidate(Valid() with { Description = new string('x', 501) }).ShouldHaveValidationErrorFor(x => x.Description);
}


/// <summary>Validator tests for CreateServiceCommand.</summary>
public class CreateServiceCommandValidatorTests
{
    private readonly CreateServiceCommandValidator _v = new();

    private static CreateServiceCommand Valid() => new("WiFi", null);

    [Fact]
    public void Valid_NoErrors() => _v.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Name_Empty_Error()
        => _v.TestValidate(Valid() with { Name = "" }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Name_TooLong_Error()
        => _v.TestValidate(Valid() with { Name = new string('x', 101) }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Description_TooLong_Error()
        => _v.TestValidate(Valid() with { Description = new string('x', 501) }).ShouldHaveValidationErrorFor(x => x.Description);

    [Fact]
    public void Description_Null_Valid()
        => _v.TestValidate(Valid() with { Description = null }).ShouldNotHaveValidationErrorFor(x => x.Description);
}
