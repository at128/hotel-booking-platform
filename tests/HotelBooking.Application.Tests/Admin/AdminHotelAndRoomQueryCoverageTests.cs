using FluentAssertions;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Features.Admin.Hotels.Command.CreateHotel;
using HotelBooking.Application.Features.Admin.Hotels.Command.UpdateHotel;
using HotelBooking.Application.Features.Admin.Rooms.Quries.GetRooms;
using HotelBooking.Application.Tests._Shared;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace HotelBooking.Application.Tests.Admin.Coverage;

public sealed class CreateAndUpdateHotelCoverageTests
{
    private readonly Mock<IAppDbContext> _db = new();

    public CreateAndUpdateHotelCoverageTests()
    {
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    [Fact]
    public async Task CreateHotel_DbUniqueViolation_ReturnsAlreadyExists()
    {
        var city = TestHelpers.CreateCity();
        _db.Setup(x => x.Cities).Returns(
            new List<City> { city }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.Hotels).Returns(
            new List<Hotel>().AsQueryable().BuildMockDbSet().Object);

        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException(
                "unique violation",
                new Exception("IX_hotels_Name_CityId")));

        var cmd = new CreateHotelCommand(
            CityId: city.Id,
            Name: " Grand Palace ",
            Owner: " Owner ",
            Address: " Address ",
            StarRating: 5,
            Description: " Desc ",
            Latitude: null,
            Longitude: null);

        var result = await new CreateHotelCommandHandler(_db.Object).Handle(cmd, default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(AdminErrors.Hotels.AlreadyExists.Code);
    }

    [Fact]
    public async Task UpdateHotel_NotFound_ReturnsNotFound()
    {
        _db.Setup(x => x.Hotels).Returns(
            new List<Hotel>().AsQueryable().BuildMockDbSet().Object);

        var cmd = new UpdateHotelCommand(
            Id: Guid.NewGuid(),
            CityId: Guid.NewGuid(),
            Name: "Updated",
            Owner: "Owner",
            Address: "Address",
            StarRating: 4,
            Description: null,
            Latitude: null,
            Longitude: null);

        var result = await new UpdateHotelCommandHandler(_db.Object).Handle(cmd, default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(AdminErrors.Hotels.NotFound.Code);
    }

    [Fact]
    public async Task UpdateHotel_CityChangeNotSupported_ReturnsValidation()
    {
        var cityA = TestHelpers.CreateCity();
        var cityB = TestHelpers.CreateCity();
        var hotel = TestHelpers.CreateHotel(cityId: cityA.Id, name: "Original");
        TestHelpers.SetNav(hotel, nameof(Hotel.City), cityA);
        TestHelpers.SetNav(hotel, nameof(Hotel.HotelRoomTypes), new List<HotelRoomType>());

        _db.Setup(x => x.Hotels).Returns(
            new List<Hotel> { hotel }.AsQueryable().BuildMockDbSet().Object);

        var cmd = new UpdateHotelCommand(
            Id: hotel.Id,
            CityId: cityB.Id,
            Name: "Updated",
            Owner: "Owner",
            Address: "Address",
            StarRating: 4,
            Description: null,
            Latitude: null,
            Longitude: null);

        var result = await new UpdateHotelCommandHandler(_db.Object).Handle(cmd, default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Admin.Hotels.CityChangeNotSupported");
    }

    [Fact]
    public async Task UpdateHotel_DuplicateNameInCity_ReturnsAlreadyExists()
    {
        var city = TestHelpers.CreateCity();
        var hotel = TestHelpers.CreateHotel(cityId: city.Id, name: "Original");
        TestHelpers.SetNav(hotel, nameof(Hotel.City), city);
        TestHelpers.SetNav(hotel, nameof(Hotel.HotelRoomTypes), new List<HotelRoomType>());

        var duplicate = TestHelpers.CreateHotel(cityId: city.Id, name: "DupName");

        _db.Setup(x => x.Hotels).Returns(
            new List<Hotel> { hotel, duplicate }.AsQueryable().BuildMockDbSet().Object);

        var cmd = new UpdateHotelCommand(
            Id: hotel.Id,
            CityId: city.Id,
            Name: " DupName ",
            Owner: "Owner",
            Address: "Address",
            StarRating: 4,
            Description: null,
            Latitude: null,
            Longitude: null);

        var result = await new UpdateHotelCommandHandler(_db.Object).Handle(cmd, default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(AdminErrors.Hotels.AlreadyExists.Code);
    }

    [Fact]
    public async Task UpdateHotel_DbUniqueViolation_ReturnsAlreadyExists()
    {
        var city = TestHelpers.CreateCity();
        var hotel = TestHelpers.CreateHotel(cityId: city.Id, name: "Original");
        TestHelpers.SetNav(hotel, nameof(Hotel.City), city);
        TestHelpers.SetNav(hotel, nameof(Hotel.HotelRoomTypes), new List<HotelRoomType>());

        _db.Setup(x => x.Hotels).Returns(
            new List<Hotel> { hotel }.AsQueryable().BuildMockDbSet().Object);

        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException(
                "unique violation",
                new Exception("IX_hotels_Name_CityId")));

        var cmd = new UpdateHotelCommand(
            Id: hotel.Id,
            CityId: city.Id,
            Name: " Updated ",
            Owner: " Owner ",
            Address: " Address ",
            StarRating: 5,
            Description: " Desc ",
            Latitude: 1.5m,
            Longitude: 2.5m);

        var result = await new UpdateHotelCommandHandler(_db.Object).Handle(cmd, default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(AdminErrors.Hotels.AlreadyExists.Code);
    }
}

public sealed class GetRoomsQueryCoverageTests
{
    private readonly Mock<IAppDbContext> _db = new();

    [Fact]
    public async Task Handle_RoomTypeAndSearchFilters_AreApplied()
    {
        var hotel = TestHelpers.CreateHotel(name: "Grand Palace");
        var suiteType = TestHelpers.CreateRoomType(name: "Suite");
        var standardType = TestHelpers.CreateRoomType(name: "Standard");

        var suiteHrt = TestHelpers.CreateHotelRoomTypeFor(hotel, suiteType);
        var standardHrt = TestHelpers.CreateHotelRoomTypeFor(hotel, standardType);

        var suiteRoom = new Room(Guid.NewGuid(), suiteHrt.Id, hotel.Id, "A101", 1, RoomStatus.Available);
        TestHelpers.SetNav(suiteRoom, nameof(Room.Hotel), hotel);
        TestHelpers.SetNav(suiteRoom, nameof(Room.HotelRoomType), suiteHrt);

        var standardRoom = new Room(Guid.NewGuid(), standardHrt.Id, hotel.Id, "B202", 2, RoomStatus.Available);
        TestHelpers.SetNav(standardRoom, nameof(Room.Hotel), hotel);
        TestHelpers.SetNav(standardRoom, nameof(Room.HotelRoomType), standardHrt);

        _db.Setup(x => x.Rooms).Returns(
            new List<Room> { suiteRoom, standardRoom }.AsQueryable().BuildMockDbSet().Object);

        var query = new GetRoomsQuery(
            HotelId: hotel.Id,
            RoomTypeId: suiteType.Id,
            Search: " suite ",
            Page: 1,
            PageSize: 20);

        var result = await new GetRoomsQueryHandler(_db.Object).Handle(query, default);

        result.IsError.Should().BeFalse();
        result.Value.Items.Should().ContainSingle(i =>
            i.HotelId == hotel.Id &&
            i.RoomTypeName == "Suite" &&
            i.RoomNumber == "A101");
    }
}
