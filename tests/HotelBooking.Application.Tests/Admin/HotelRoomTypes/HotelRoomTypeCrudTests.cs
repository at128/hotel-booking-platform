/// <summary>
/// Tests for Admin HotelRoomType CRUD handlers.
/// </summary>
using FluentAssertions;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Features.Admin.HotelRoomTypes.Commands.CreateHotelRoomType;
using HotelBooking.Application.Features.Admin.HotelRoomTypes.Commands.DeleteHotelRoomType;
using HotelBooking.Application.Features.Admin.HotelRoomTypes.Commands.UpdateHotelRoomType;
using HotelBooking.Application.Features.Admin.HotelRoomTypes.Queries.GetHotelRoomTypeById;
using HotelBooking.Application.Features.Admin.HotelRoomTypes.Queries.GetHotelRoomTypes;
using HotelBooking.Application.Tests._Shared;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace HotelBooking.Application.Tests.Admin.HotelRoomTypes;

public sealed class HotelRoomTypeCrudTests
{
    private readonly Mock<IAppDbContext> _db = new();

    private CreateHotelRoomTypeCommandHandler CreateCreateHandler() => new(_db.Object);
    private GetHotelRoomTypesQueryHandler CreateGetAllHandler() => new(_db.Object);
    private GetHotelRoomTypeByIdQueryHandler CreateGetByIdHandler() => new(_db.Object);
    private UpdateHotelRoomTypeCommandHandler CreateUpdateHandler() => new(_db.Object);
    private DeleteHotelRoomTypeCommandHandler CreateDeleteHandler() => new(_db.Object);

    private void SetupHotels(List<Hotel> items)
    {
        var mock = items.AsQueryable().BuildMockDbSet();
        _db.Setup(x => x.Hotels).Returns(mock.Object);
    }

    private void SetupRoomTypes(List<RoomType> items)
    {
        var mock = items.AsQueryable().BuildMockDbSet();
        _db.Setup(x => x.RoomTypes).Returns(mock.Object);
    }

    private void SetupHotelRoomTypes(List<HotelRoomType> items)
    {
        var mock = items.AsQueryable().BuildMockDbSet();
        _db.Setup(x => x.HotelRoomTypes).Returns(mock.Object);
    }

    private void SetupBookingRooms(List<BookingRoom> items)
    {
        var mock = items.AsQueryable().BuildMockDbSet();
        _db.Setup(x => x.BookingRooms).Returns(mock.Object);
    }

    private void SetupCheckoutHolds(List<CheckoutHold> items)
    {
        var mock = items.AsQueryable().BuildMockDbSet();
        _db.Setup(x => x.CheckoutHolds).Returns(mock.Object);
    }

    private void SetupRooms(List<Room> items)
    {
        var mock = items.AsQueryable().BuildMockDbSet();
        _db.Setup(x => x.Rooms).Returns(mock.Object);
    }

    private void SetupSaveChanges(int affected = 1)
    {
        _db.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(affected);
    }

    [Fact]
    public async Task Create_Success_ReturnsCreatedId()
    {
        var hotel = TestHelpers.CreateHotel();
        var roomType = TestHelpers.CreateRoomType();

        SetupHotels([hotel]);
        SetupRoomTypes([roomType]);
        SetupHotelRoomTypes([]);
        SetupSaveChanges();

        var cmd = new CreateHotelRoomTypeCommand(
            HotelId: hotel.Id,
            RoomTypeId: roomType.Id,
            PricePerNight: 150m,
            AdultCapacity: 2,
            ChildCapacity: 1,
            MaxOccupancy: 3,
            Description: "Deluxe city view");

        var result = await CreateCreateHandler().Handle(cmd, default);

        result.IsError.Should().BeFalse();
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Create_HotelNotFound_ReturnsError()
    {
        var roomType = TestHelpers.CreateRoomType();

        SetupHotels([]);
        SetupRoomTypes([roomType]);
        SetupHotelRoomTypes([]);

        var cmd = new CreateHotelRoomTypeCommand(
            HotelId: Guid.NewGuid(),
            RoomTypeId: roomType.Id,
            PricePerNight: 150m,
            AdultCapacity: 2,
            ChildCapacity: 1,
            MaxOccupancy: 3,
            Description: "Deluxe");

        var result = await CreateCreateHandler().Handle(cmd, default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(AdminErrors.Hotels.NotFound.Code);
    }

    [Fact]
    public async Task Create_RoomTypeNotFound_ReturnsError()
    {
        var hotel = TestHelpers.CreateHotel();

        SetupHotels([hotel]);
        SetupRoomTypes([]);
        SetupHotelRoomTypes([]);

        var cmd = new CreateHotelRoomTypeCommand(
            HotelId: hotel.Id,
            RoomTypeId: Guid.NewGuid(),
            PricePerNight: 150m,
            AdultCapacity: 2,
            ChildCapacity: 1,
            MaxOccupancy: 3,
            Description: "Deluxe");

        var result = await CreateCreateHandler().Handle(cmd, default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(AdminErrors.RoomTypes.NotFound(cmd.RoomTypeId).Code);
    }

    [Fact]
    public async Task Create_DuplicateHotelRoomType_ReturnsConflict()
    {
        var hotel = TestHelpers.CreateHotel();
        var roomType = TestHelpers.CreateRoomType();
        var existing = TestHelpers.CreateHotelRoomType(
            hotelId: hotel.Id,
            roomTypeId: roomType.Id);

        SetupHotels([hotel]);
        SetupRoomTypes([roomType]);
        SetupHotelRoomTypes([existing]);

        var cmd = new CreateHotelRoomTypeCommand(
            HotelId: hotel.Id,
            RoomTypeId: roomType.Id,
            PricePerNight: 150m,
            AdultCapacity: 2,
            ChildCapacity: 1,
            MaxOccupancy: 3,
            Description: "Already exists");

        var result = await CreateCreateHandler().Handle(cmd, default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(AdminErrors.HotelRoomTypes.AlreadyExists.Code);
    }

    [Fact]
    public async Task GetAll_WithoutFilter_ReturnsAllActiveHotelRoomTypes()
    {
        var h1 = TestHelpers.CreateHotel();
        var h2 = TestHelpers.CreateHotel();

        var rt1 = TestHelpers.CreateHotelRoomType(hotelId: h1.Id);
        var rt2 = TestHelpers.CreateHotelRoomType(hotelId: h2.Id);

        SetupHotelRoomTypes([rt1, rt2]);

        var result = await CreateGetAllHandler().Handle(new GetHotelRoomTypesQuery(), default);

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_WithHotelFilter_ReturnsOnlyMatchingHotelRoomTypes()
    {
        var hotelA = TestHelpers.CreateHotel();
        var hotelB = TestHelpers.CreateHotel();

        var rtA = TestHelpers.CreateHotelRoomType(hotelId: hotelA.Id);
        var rtB = TestHelpers.CreateHotelRoomType(hotelId: hotelB.Id);

        SetupHotelRoomTypes([rtA, rtB]);

        var result = await CreateGetAllHandler().Handle(
            new GetHotelRoomTypesQuery(hotelA.Id), default);

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        result.Value[0].HotelId.Should().Be(hotelA.Id);
    }

    [Fact]
    public async Task GetById_Existing_ReturnsDto()
    {
        var entity = TestHelpers.CreateHotelRoomType();
        SetupHotelRoomTypes([entity]);

        var result = await CreateGetByIdHandler().Handle(
            new GetHotelRoomTypeByIdQuery(entity.Id), default);

        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(entity.Id);
        result.Value.HotelId.Should().Be(entity.HotelId);
        result.Value.RoomTypeId.Should().Be(entity.RoomTypeId);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsError()
    {
        SetupHotelRoomTypes([]);
        var id = Guid.NewGuid();

        var result = await CreateGetByIdHandler().Handle(
            new GetHotelRoomTypeByIdQuery(id), default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(AdminErrors.HotelRoomTypes.NotFound(id).Code);
    }

    [Fact]
    public async Task Update_Existing_UpdatesFields()
    {
        var entity = TestHelpers.CreateHotelRoomType(
            pricePerNight: 100m,
            adultCapacity: 2,
            childCapacity: 0,
            maxOccupancy: 2,
            description: "Old");

        SetupHotelRoomTypes([entity]);
        SetupSaveChanges();

        var cmd = new UpdateHotelRoomTypeCommand(
            Id: entity.Id,
            PricePerNight: 180m,
            AdultCapacity: 2,
            ChildCapacity: 2,
            MaxOccupancy: 3,
            Description: "Updated");

        var result = await CreateUpdateHandler().Handle(cmd, default);

        result.IsError.Should().BeFalse();
        entity.PricePerNight.Should().Be(180m);
        entity.AdultCapacity.Should().Be(2);
        entity.ChildCapacity.Should().Be(2);
        entity.MaxOccupancy.Should().Be(3);
        entity.Description.Should().Be("Updated");
    }

    [Fact]
    public async Task Update_NotFound_ReturnsError()
    {
        SetupHotelRoomTypes([]);

        var id = Guid.NewGuid();

        var cmd = new UpdateHotelRoomTypeCommand(
            Id: id,
            PricePerNight: 180m,
            AdultCapacity: 2,
            ChildCapacity: 2,
            MaxOccupancy: 3,
            Description: "Updated");

        var result = await CreateUpdateHandler().Handle(cmd, default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(AdminErrors.HotelRoomTypes.NotFound(id).Code);
    }

    [Fact]
    public async Task Delete_ExistingWithoutDependencies_SoftDeletes()
    {
        var entity = TestHelpers.CreateHotelRoomType();
        SetupHotelRoomTypes([entity]);
        SetupBookingRooms([]);
        SetupCheckoutHolds([]);
        SetupRooms([]);
        SetupSaveChanges();

        var result = await CreateDeleteHandler().Handle(
            new DeleteHotelRoomTypeCommand(entity.Id), default);

        result.IsError.Should().BeFalse();
        entity.DeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_NotFound_ReturnsError()
    {
        SetupHotelRoomTypes([]);
        SetupBookingRooms([]);
        SetupCheckoutHolds([]);
        SetupRooms([]);

        var id = Guid.NewGuid();

        var result = await CreateDeleteHandler().Handle(
            new DeleteHotelRoomTypeCommand(id), default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(AdminErrors.HotelRoomTypes.NotFound(id).Code);
    }

    [Fact]
    public async Task Delete_WithPendingBookings_ReturnsConflict()
    {
        var entity = TestHelpers.CreateHotelRoomType();
        var booking = TestHelpers.CreateBooking(status: BookingStatus.Confirmed);

        var bookingRoom = TestHelpers.CreateBookingRoom(
            hotelId: booking.HotelId,
            hotelRoomTypeId: entity.Id,
            booking: booking);

        SetupHotelRoomTypes([entity]);
        SetupBookingRooms([bookingRoom]);
        SetupCheckoutHolds([]);
        SetupRooms([]);

        var result = await CreateDeleteHandler().Handle(
            new DeleteHotelRoomTypeCommand(entity.Id), default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(AdminErrors.HotelRoomTypes.HasPendingBookings.Code);
    }

    [Fact]
    public async Task Delete_WithActiveHolds_ReturnsConflict()
    {
        var entity = TestHelpers.CreateHotelRoomType();

        var hold = TestHelpers.CreateHold(
            hotelRoomTypeId: entity.Id,
            expiryMinutes: 30);

        SetupHotelRoomTypes([entity]);
        SetupBookingRooms([]);
        SetupCheckoutHolds([hold]);
        SetupRooms([]);

        var result = await CreateDeleteHandler().Handle(
            new DeleteHotelRoomTypeCommand(entity.Id), default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(AdminErrors.HotelRoomTypes.HasActiveHolds.Code);
    }

    [Fact]
    public async Task Delete_WithAssignedRooms_ReturnsConflict()
    {
        var entity = TestHelpers.CreateHotelRoomType();

        var room = TestHelpers.CreateRoom(
            hotelRoomTypeId: entity.Id,
            hotelId: entity.HotelId);

        SetupHotelRoomTypes([entity]);
        SetupBookingRooms([]);
        SetupCheckoutHolds([]);
        SetupRooms([room]);

        var result = await CreateDeleteHandler().Handle(
            new DeleteHotelRoomTypeCommand(entity.Id), default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(AdminErrors.HotelRoomTypes.HasAssignedRooms.Code);
    }
}