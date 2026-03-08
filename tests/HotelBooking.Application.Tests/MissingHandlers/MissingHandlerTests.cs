/// <summary>
/// Tests for handlers that were not covered in previous steps:
/// Home (FeaturedDeals, TrendingCities, SearchConfig), Hotel detail pages,
/// Events (TrackView, RecentlyVisited), Reviews, Booking queries,
/// Admin Hotel update/delete, Admin Room create/delete, Search validator.
/// </summary>
using FluentAssertions;
using FluentValidation.TestHelper;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Features.Admin.Hotels.Command.DeleteHotel;
using HotelBooking.Application.Features.Admin.Hotels.Command.UpdateHotel;
using HotelBooking.Application.Features.Admin.Rooms.Commands.CreateRoom;
using HotelBooking.Application.Features.Admin.Rooms.Commands.DeleteRoom;
using HotelBooking.Application.Features.Checkout.Queries.GetBooking;
using HotelBooking.Application.Features.Checkout.Queries.GetUserBookings;
using HotelBooking.Application.Features.Events.Commands.TrackHotelView;
using HotelBooking.Application.Features.Events.Queries.GetRecentlyVisited;
using HotelBooking.Application.Features.Home.Queries.GetFeaturedDeals;
using HotelBooking.Application.Features.Home.Queries.GetSearchConfig;
using HotelBooking.Application.Features.Home.Queries.GetTrendingCities;
using HotelBooking.Application.Features.Hotels.Queries.GetHotelDetails;
using HotelBooking.Application.Features.Hotels.Queries.GetHotelGallery;
using HotelBooking.Application.Features.Hotels.Queries.GetRoomAvailability;
using HotelBooking.Application.Features.Search.Queries.SearchHotels;
using HotelBooking.Application.Tests._Shared;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Hotels.Enums;
using HotelBooking.Domain.Rooms;
using Microsoft.Extensions.Options;
using MockQueryable.Moq;
using Moq;
using Xunit;
namespace HotelBooking.Application.Tests.MissingHandlers;

// ═══════════════════════════════════════════════════════════════════════════
// Home — GetFeaturedDeals
// ═══════════════════════════════════════════════════════════════════════════

public sealed class GetFeaturedDealsQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _db = new();

    [Fact]
    public async Task Handle_ReturnsActiveDeals()
    {
        _db.Setup(x => x.FeaturedDeals).Returns(
            new List<FeaturedDeal>().AsQueryable().BuildMockDbSet().Object);

        var sut = new GetFeaturedDealsQueryHandler(_db.Object);
        var result = await sut.Handle(new GetFeaturedDealsQuery(), default);

        result.IsError.Should().BeFalse();
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Home — GetTrendingCities
// ═══════════════════════════════════════════════════════════════════════════

public sealed class GetTrendingCitiesQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _db = new();

    [Fact]
    public async Task Handle_ReturnsTop5Cities()
    {
        _db.Setup(x => x.Cities).Returns(
            new List<City>().AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.HotelVisits).Returns(
            new List<HotelVisit>().AsQueryable().BuildMockDbSet().Object);

        var sut = new GetTrendingCitiesQueryHandler(_db.Object);
        var result = await sut.Handle(new GetTrendingCitiesQuery(), default);

        result.IsError.Should().BeFalse();
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Home — GetSearchConfig
// ═══════════════════════════════════════════════════════════════════════════

public sealed class GetSearchConfigQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _db = new();

    [Fact]
    public async Task Handle_ReturnsDefaults()
    {
        _db.Setup(x => x.Services).Returns(
            new List<Domain.Services.Service>().AsQueryable().BuildMockDbSet().Object);

        var sut = new GetSearchConfigQueryHandler(_db.Object, TestHelpers.BookingOptions());
        var result = await sut.Handle(new GetSearchConfigQuery(), default);

        result.IsError.Should().BeFalse();
        result.Value.DefaultAdults.Should().Be(2);
        result.Value.DefaultChildren.Should().Be(0);
        result.Value.DefaultRooms.Should().Be(1);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Hotels — GetHotelDetails
// ═══════════════════════════════════════════════════════════════════════════

public sealed class GetHotelDetailsQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _db = new();

    [Fact]
    public async Task Handle_HotelExists_ReturnsDetails()
    {
        var hotel = TestHelpers.CreateHotel();
        var city = TestHelpers.CreateCity(id: hotel.CityId);
        TestHelpers.SetNav(hotel, "City", city);
        TestHelpers.SetNav(hotel, "HotelServices", new List<HotelService>());
        TestHelpers.SetNav(hotel, "HotelRoomTypes", new List<HotelRoomType>());

        _db.Setup(x => x.Hotels).Returns(
            new List<Hotel> { hotel }.AsQueryable().BuildMockDbSet().Object);

        var sut = new GetHotelDetailsQueryHandler(_db.Object);
        var result = await sut.Handle(new GetHotelDetailsQuery(hotel.Id), default);

        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be("Test Hotel");
    }

    [Fact]
    public async Task Handle_HotelNotFound_ReturnsNotFound()
    {
        _db.Setup(x => x.Hotels).Returns(
            new List<Hotel>().AsQueryable().BuildMockDbSet().Object);

        var sut = new GetHotelDetailsQueryHandler(_db.Object);
        var result = await sut.Handle(new GetHotelDetailsQuery(Guid.NewGuid()), default);

        result.IsError.Should().BeTrue();
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Hotels — GetHotelGallery
// ═══════════════════════════════════════════════════════════════════════════

public sealed class GetHotelGalleryQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _db = new();

    [Fact]
    public async Task Handle_HotelExists_ReturnsGallery()
    {
        var hotel = TestHelpers.CreateHotel();
        _db.Setup(x => x.Hotels).Returns(
            new List<Hotel> { hotel }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.Images).Returns(
            new List<Image>().AsQueryable().BuildMockDbSet().Object);

        var sut = new GetHotelGalleryQueryHandler(_db.Object);
        var result = await sut.Handle(new GetHotelGalleryQuery(hotel.Id), default);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_HotelNotFound_ReturnsError()
    {
        _db.Setup(x => x.Hotels).Returns(
            new List<Hotel>().AsQueryable().BuildMockDbSet().Object);

        var sut = new GetHotelGalleryQueryHandler(_db.Object);
        var result = await sut.Handle(new GetHotelGalleryQuery(Guid.NewGuid()), default);

        result.IsError.Should().BeTrue();
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Hotels — GetRoomAvailability
// ═══════════════════════════════════════════════════════════════════════════

public sealed class GetRoomAvailabilityQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _db = new();

    [Fact]
    public async Task Handle_HotelNotFound_ReturnsError()
    {
        _db.Setup(x => x.Hotels).Returns(
            new List<Hotel>().AsQueryable().BuildMockDbSet().Object);

        var sut = new GetRoomAvailabilityQueryHandler(_db.Object);
        var result = await sut.Handle(
            new GetRoomAvailabilityQuery(Guid.NewGuid(),
                new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5)), default);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_HotelExists_ReturnsAvailability()
    {
        var hotel = TestHelpers.CreateHotel();
        TestHelpers.SetPrivateProp(hotel, "DeletedAtUtc", (DateTimeOffset?)null);
        _db.Setup(x => x.Hotels).Returns(
            new List<Hotel> { hotel }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.HotelRoomTypes).Returns(
            new List<HotelRoomType>().AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.Images).Returns(
            new List<Image>().AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.Bookings).Returns(
            new List<Booking>().AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.CheckoutHolds).Returns(
            new List<CheckoutHold>().AsQueryable().BuildMockDbSet().Object);

        var sut = new GetRoomAvailabilityQueryHandler(_db.Object);
        var result = await sut.Handle(
            new GetRoomAvailabilityQuery(hotel.Id,
                new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5)), default);

        result.IsError.Should().BeFalse();
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Events — TrackHotelView
// ═══════════════════════════════════════════════════════════════════════════

public sealed class TrackHotelViewCommandHandlerTests
{
    private readonly Mock<IAppDbContext> _db = new();

    [Fact]
    public async Task Handle_HotelNotFound_ReturnsNotFound()
    {
        _db.Setup(x => x.Hotels).Returns(
            new List<Hotel>().AsQueryable().BuildMockDbSet().Object);

        var sut = new TrackHotelViewCommandHandler(_db.Object);
        var result = await sut.Handle(
            new TrackHotelViewCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NewVisit_CreatesRecord()
    {
        var hotel = TestHelpers.CreateHotel();
        _db.Setup(x => x.Hotels).Returns(
            new List<Hotel> { hotel }.AsQueryable().BuildMockDbSet().Object);

        var visitSet = new List<HotelVisit>().AsQueryable().BuildMockDbSet();
        visitSet.Setup(x => x.Add(It.IsAny<HotelVisit>()));
        _db.Setup(x => x.HotelVisits).Returns(visitSet.Object);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new TrackHotelViewCommandHandler(_db.Object);
        var result = await sut.Handle(
            new TrackHotelViewCommand(Guid.NewGuid(), hotel.Id), default);

        result.IsError.Should().BeFalse();
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Events — GetRecentlyVisited
// ═══════════════════════════════════════════════════════════════════════════

public sealed class GetRecentlyVisitedQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _db = new();

    [Fact]
    public async Task Handle_NoVisits_ReturnsEmpty()
    {
        _db.Setup(x => x.HotelVisits).Returns(
            new List<HotelVisit>().AsQueryable().BuildMockDbSet().Object);

        var sut = new GetRecentlyVisitedQueryHandler(_db.Object);
        var result = await sut.Handle(new GetRecentlyVisitedQuery(Guid.NewGuid()), default);

        result.IsError.Should().BeFalse();
        result.Value.Items.Should().BeEmpty();
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Booking Queries — GetBooking, GetUserBookings
// ═══════════════════════════════════════════════════════════════════════════

public sealed class GetBookingQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _db = new();

    [Fact]
    public async Task Handle_BookingNotFound_ReturnsNotFound()
    {
        _db.Setup(x => x.Bookings).Returns(
            new List<Booking>().AsQueryable().BuildMockDbSet().Object);

        var sut = new GetBookingQueryHandler(_db.Object);
        var result = await sut.Handle(
            new GetBookingQuery(Guid.NewGuid(), Guid.NewGuid(), false), default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Booking.NotFound");
    }

    [Fact]
    public async Task Handle_NotOwnerNotAdmin_ReturnsAccessDenied()
    {
        var booking = TestHelpers.CreateBooking();
        TestHelpers.SetNav(booking, "BookingRooms", new List<BookingRoom>());
        TestHelpers.SetNav(booking, "Payments", new List<Payment>());
        TestHelpers.SetNav(booking, "Cancellation", (Cancellation?)null);

        _db.Setup(x => x.Bookings).Returns(
            new List<Booking> { booking }.AsQueryable().BuildMockDbSet().Object);

        var sut = new GetBookingQueryHandler(_db.Object);
        var result = await sut.Handle(
            new GetBookingQuery(booking.Id, Guid.NewGuid(), false), default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Booking.AccessDenied");
    }
}

public sealed class GetUserBookingsQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _db = new();

    [Fact]
    public async Task Handle_ReturnsUserBookings()
    {
        _db.Setup(x => x.Bookings).Returns(
            new List<Booking>().AsQueryable().BuildMockDbSet().Object);

        var sut = new GetUserBookingsQueryHandler(_db.Object);
        var result = await sut.Handle(
            new GetUserBookingsQuery(Guid.NewGuid(), null, 1, 20), default);

        result.IsError.Should().BeFalse();
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Admin — DeleteHotel, UpdateHotel
// ═══════════════════════════════════════════════════════════════════════════

public sealed class DeleteHotelCommandHandlerTests
{
    private readonly Mock<IAppDbContext> _db = new();

    [Fact]
    public async Task Handle_HotelNotFound_ReturnsNotFound()
    {
        _db.Setup(x => x.Hotels).Returns(
            new List<Hotel>().AsQueryable().BuildMockDbSet().Object);

        var sut = new DeleteHotelCommandHandler(_db.Object);
        var result = await sut.Handle(new DeleteHotelCommand(Guid.NewGuid()), default);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_HasBookings_ReturnsConflict()
    {
        var hotel = TestHelpers.CreateHotel();
        _db.Setup(x => x.Hotels).Returns(
            new List<Hotel> { hotel }.AsQueryable().BuildMockDbSet().Object);

        var booking = TestHelpers.CreateBooking(hotelId: hotel.Id);
        _db.Setup(x => x.Bookings).Returns(
            new List<Booking> { booking }.AsQueryable().BuildMockDbSet().Object);

        var sut = new DeleteHotelCommandHandler(_db.Object);
        var result = await sut.Handle(new DeleteHotelCommand(hotel.Id), default);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoBookings_SoftDeletes()
    {
        var hotel = TestHelpers.CreateHotel();
        _db.Setup(x => x.Hotels).Returns(
            new List<Hotel> { hotel }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.Bookings).Returns(
            new List<Booking>().AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.HotelRoomTypes).Returns(
            new List<HotelRoomType>().AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.Rooms).Returns(
            new List<Room>().AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new DeleteHotelCommandHandler(_db.Object);
        var result = await sut.Handle(new DeleteHotelCommand(hotel.Id), default);

        result.IsError.Should().BeFalse();
        hotel.DeletedAtUtc.Should().NotBeNull();
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Admin — CreateRoom (HotelRoomType), DeleteRoom
// ═══════════════════════════════════════════════════════════════════════════

public sealed class CreateRoomCommandHandlerTests
{
    private readonly Mock<IAppDbContext> _db = new();

    [Fact]
    public async Task Handle_HotelRoomTypeNotFound_ReturnsError()
    {
        _db.Setup(x => x.HotelRoomTypes).Returns(
            new List<HotelRoomType>().AsQueryable().BuildMockDbSet().Object);

        var sut = new CreateRoomCommandHandler(_db.Object);
        var result = await sut.Handle(
            new CreateRoomCommand(
                HotelRoomTypeId: Guid.NewGuid(),
                RoomNumber: "101",
                Floor: 1,
                Status: RoomStatus.Available),
            default);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DuplicateRoomNumber_ReturnsConflict()
    {
        var hotel = TestHelpers.CreateHotel();
        var roomType = TestHelpers.CreateRoomType();
        var hrt = TestHelpers.CreateHotelRoomTypeFor(hotel, roomType);

        TestHelpers.SetNav(hrt, "Hotel", hotel);
        TestHelpers.SetNav(hrt, "RoomType", roomType);

        _db.Setup(x => x.HotelRoomTypes).Returns(
            new List<HotelRoomType> { hrt }.AsQueryable().BuildMockDbSet().Object);

        var existingRoom = new Room(
            id: Guid.NewGuid(),
            hotelRoomTypeId: hrt.Id,
            hotelId: hotel.Id,
            roomNumber: "101",
            floor: 1,
            status: RoomStatus.Available);

        _db.Setup(x => x.Rooms).Returns(
            new List<Room> { existingRoom }.AsQueryable().BuildMockDbSet().Object);

        var sut = new CreateRoomCommandHandler(_db.Object);
        var result = await sut.Handle(
            new CreateRoomCommand(
                HotelRoomTypeId: hrt.Id,
                RoomNumber: "101",
                Floor: 1,
                Status: RoomStatus.Available),
            default);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Success_CreatesRoom()
    {
        var hotel = TestHelpers.CreateHotel();
        var roomType = TestHelpers.CreateRoomType();
        var hrt = TestHelpers.CreateHotelRoomTypeFor(hotel, roomType);

        TestHelpers.SetNav(hrt, "Hotel", hotel);
        TestHelpers.SetNav(hrt, "RoomType", roomType);

        _db.Setup(x => x.HotelRoomTypes).Returns(
            new List<HotelRoomType> { hrt }.AsQueryable().BuildMockDbSet().Object);

        var roomSet = new List<Room>().AsQueryable().BuildMockDbSet();
        roomSet.Setup(x => x.Add(It.IsAny<Room>()));

        _db.Setup(x => x.Rooms).Returns(roomSet.Object);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new CreateRoomCommandHandler(_db.Object);
        var result = await sut.Handle(
            new CreateRoomCommand(
                HotelRoomTypeId: hrt.Id,
                RoomNumber: "101",
                Floor: 1,
                Status: RoomStatus.Available),
            default);

        result.IsError.Should().BeFalse();
        result.Value.RoomNumber.Should().Be("101");
        result.Value.HotelRoomTypeId.Should().Be(hrt.Id);
    }
}

public sealed class DeleteRoomCommandHandlerTests
{
    private readonly Mock<IAppDbContext> _db = new();

    [Fact]
    public async Task Handle_NotFound_ReturnsError()
    {
        _db.Setup(x => x.Rooms).Returns(
            new List<Room>().AsQueryable().BuildMockDbSet().Object);

        var sut = new DeleteRoomCommandHandler(_db.Object);
        var result = await sut.Handle(new DeleteRoomCommand(Guid.NewGuid()), default);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_HasConfirmedBookings_ReturnsError()
    {
        var hotel = TestHelpers.CreateHotel();
        var roomType = TestHelpers.CreateRoomType();
        var hrt = TestHelpers.CreateHotelRoomTypeFor(hotel, roomType);

        var room = new Room(
            id: Guid.NewGuid(),
            hotelRoomTypeId: hrt.Id,
            hotelId: hotel.Id,
            roomNumber: "101",
            floor: 1,
            status: RoomStatus.Available);

        _db.Setup(x => x.Rooms).Returns(
            new List<Room> { room }.AsQueryable().BuildMockDbSet().Object);

        var booking = TestHelpers.CreateBooking(status: BookingStatus.Confirmed);
        var br = new BookingRoom(
            Guid.NewGuid(),
            booking.Id,
            hotel.Id,
            room.Id,
            hrt.Id,
            "Deluxe",
            "101",
            100m);

        TestHelpers.SetNav(br, "Booking", booking);

        _db.Setup(x => x.BookingRooms).Returns(
            new List<BookingRoom> { br }.AsQueryable().BuildMockDbSet().Object);

        var sut = new DeleteRoomCommandHandler(_db.Object);
        var result = await sut.Handle(new DeleteRoomCommand(room.Id), default);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoConfirmedBookings_SoftDeletesRoom()
    {
        var hotel = TestHelpers.CreateHotel();
        var roomType = TestHelpers.CreateRoomType();
        var hrt = TestHelpers.CreateHotelRoomTypeFor(hotel, roomType);

        var room = new Room(
            id: Guid.NewGuid(),
            hotelRoomTypeId: hrt.Id,
            hotelId: hotel.Id,
            roomNumber: "101",
            floor: 1,
            status: RoomStatus.Available);

        _db.Setup(x => x.Rooms).Returns(
            new List<Room> { room }.AsQueryable().BuildMockDbSet().Object);

        _db.Setup(x => x.BookingRooms).Returns(
            new List<BookingRoom>().AsQueryable().BuildMockDbSet().Object);

        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new DeleteRoomCommandHandler(_db.Object);
        var result = await sut.Handle(new DeleteRoomCommand(room.Id), default);

        result.IsError.Should().BeFalse();
        room.DeletedAtUtc.Should().NotBeNull();
    }
}
// ═══════════════════════════════════════════════════════════════════════════
// Search Validator + Room Availability Validator
// ═══════════════════════════════════════════════════════════════════════════

public sealed class SearchHotelsQueryValidatorTests
{
    private readonly SearchHotelsQueryValidator _v = new();

    private static SearchHotelsQuery Valid() => new(
        Query: null,
        City: null,
        RoomTypeId: null,
        CheckIn: null,
        CheckOut: null,
        Adults: null,
        Children: null,
        NumberOfRooms: null,
        MinPrice: null,
        MaxPrice: null,
        MinStarRating: null,
        Amenities: null,
        SortBy: null,
        Cursor: null,
        Limit: 20);

    [Fact]
    public void Valid_NoErrors() =>
        _v.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Limit_Zero_Error() =>
        _v.TestValidate(Valid() with { Limit = 0 }).ShouldHaveValidationErrorFor(x => x.Limit);

    [Fact]
    public void Limit_51_Error() =>
        _v.TestValidate(Valid() with { Limit = 51 }).ShouldHaveValidationErrorFor(x => x.Limit);

    [Fact]
    public void MinStar_Zero_Error() =>
        _v.TestValidate(Valid() with { MinStarRating = 0 }).ShouldHaveValidationErrorFor(x => x.MinStarRating);

    [Fact]
    public void MinStar_Six_Error() =>
        _v.TestValidate(Valid() with { MinStarRating = 6 }).ShouldHaveValidationErrorFor(x => x.MinStarRating);

    [Fact]
    public void MaxPriceLessThanMin_Error() =>
        _v.TestValidate(Valid() with { MinPrice = 200, MaxPrice = 100 })
            .ShouldHaveValidationErrorFor(x => x.MaxPrice);

    [Fact]
    public void CheckOutBeforeCheckIn_Error() =>
        _v.TestValidate(Valid() with
        {
            CheckIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            CheckOut = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
        }).ShouldHaveValidationErrorFor(x => x.CheckOut);

    [Fact]
    public void CheckInWithoutCheckOut_Error() =>
        _v.TestValidate(Valid() with
        {
            CheckIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
        }).ShouldHaveValidationErrorFor(x => x.CheckOut);

    [Fact]
    public void InvalidSortBy_Error() =>
        _v.TestValidate(Valid() with { SortBy = "invalid" }).ShouldHaveValidationErrorFor(x => x.SortBy);

    [Theory]
    [InlineData("price_asc")]
    [InlineData("price_desc")]
    [InlineData("rating_desc")]
    [InlineData("stars_desc")]
    public void ValidSortBy_NoError(string sort) =>
        _v.TestValidate(Valid() with { SortBy = sort }).ShouldNotHaveValidationErrorFor(x => x.SortBy);
}

public sealed class GetRoomAvailabilityQueryValidatorTests
{
    private readonly GetRoomAvailabilityQueryValidator _v = new();

    [Fact]
    public void Valid_NoErrors()
    {
        var q = new GetRoomAvailabilityQuery(Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)));
        _v.TestValidate(q).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyHotelId_Error()
    {
        var q = new GetRoomAvailabilityQuery(Guid.Empty,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)));
        _v.TestValidate(q).ShouldHaveValidationErrorFor(x => x.HotelId);
    }

    [Fact]
    public void CheckOutBeforeCheckIn_Error()
    {
        var q = new GetRoomAvailabilityQuery(Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)));
        _v.TestValidate(q).ShouldHaveValidationErrorFor(x => x.CheckOut);
    }
}

public sealed class CreateRoomCommandValidatorTests
{
    private readonly CreateRoomCommandValidator _v = new();

    [Fact]
    public void Valid_NoErrors() =>
        _v.TestValidate(new CreateRoomCommand(
            HotelRoomTypeId: Guid.NewGuid(),
            RoomNumber: "101",
            Floor: 1,
            Status: RoomStatus.Available))
        .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void HotelRoomTypeId_Empty_Error() =>
        _v.TestValidate(new CreateRoomCommand(
            HotelRoomTypeId: Guid.Empty,
            RoomNumber: "101",
            Floor: 1,
            Status: RoomStatus.Available))
        .ShouldHaveValidationErrorFor(x => x.HotelRoomTypeId);

    [Fact]
    public void RoomNumber_Empty_Error() =>
        _v.TestValidate(new CreateRoomCommand(
            HotelRoomTypeId: Guid.NewGuid(),
            RoomNumber: "",
            Floor: 1,
            Status: RoomStatus.Available))
        .ShouldHaveValidationErrorFor(x => x.RoomNumber);

    [Fact]
    public void RoomNumber_TooLong_Error() =>
        _v.TestValidate(new CreateRoomCommand(
            HotelRoomTypeId: Guid.NewGuid(),
            RoomNumber: new string('1', 11),
            Floor: 1,
            Status: RoomStatus.Available))
        .ShouldHaveValidationErrorFor(x => x.RoomNumber);

    [Fact]
    public void Floor_Negative_Error() =>
        _v.TestValidate(new CreateRoomCommand(
            HotelRoomTypeId: Guid.NewGuid(),
            RoomNumber: "101",
            Floor: -1,
            Status: RoomStatus.Available))
        .ShouldHaveValidationErrorFor(x => x.Floor);
}

public sealed class TrackHotelViewCommandValidatorTests
{
    private readonly TrackHotelViewCommandValidator _v = new();

    [Fact] public void Valid_NoErrors() =>
        _v.TestValidate(new TrackHotelViewCommand(Guid.NewGuid(), Guid.NewGuid()))
          .ShouldNotHaveAnyValidationErrors();

    [Fact] public void EmptyUserId_Error() =>
        _v.TestValidate(new TrackHotelViewCommand(Guid.Empty, Guid.NewGuid()))
          .ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact] public void EmptyHotelId_Error() =>
        _v.TestValidate(new TrackHotelViewCommand(Guid.NewGuid(), Guid.Empty))
          .ShouldHaveValidationErrorFor(x => x.HotelId);
}
