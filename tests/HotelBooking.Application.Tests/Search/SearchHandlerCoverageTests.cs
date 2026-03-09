using FluentAssertions;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Features.Search.Queries.SearchHotels;
using HotelBooking.Application.Tests._Shared;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Hotels.Enums;
using HotelBooking.Domain.Rooms;
using HotelBooking.Domain.Services;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace HotelBooking.Application.Tests.Search;

public sealed class SearchHotelsQueryHandlerCoverageTests
{
    private readonly Mock<IAppDbContext> _db = new();
    private readonly Mock<IHotelSearchService> _search = new();
    private readonly Mock<ILogger<SearchHotelsQueryHandler>> _log = new();

    private SearchHotelsQueryHandler Sut() => new(_db.Object, _search.Object, _log.Object);

    private void SetupDb(List<Hotel> hotels)
    {
        _db.Setup(x => x.Hotels).Returns(hotels.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.BookingRooms).Returns(new List<BookingRoom>().AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.CheckoutHolds).Returns(new List<CheckoutHold>().AsQueryable().BuildMockDbSet().Object);
        _search.Setup(x => x.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
    }

    private static (Hotel hotel, City city, RoomType roomType) BuildHotel(
        string name,
        string cityName,
        short stars,
        decimal minPrice,
        double rating,
        short adultCapacity = 2,
        short childCapacity = 0,
        params string[] amenities)
    {
        var city = TestHelpers.CreateCity(name: cityName, country: "IL");
        var hotel = TestHelpers.CreateHotel(cityId: city.Id, name: name, starRating: stars);
        TestHelpers.SetNav(hotel, nameof(Hotel.City), city);

        TestHelpers.SetPrivateProp(hotel, nameof(Hotel.MinPricePerNight), minPrice);
        TestHelpers.SetPrivateProp(hotel, nameof(Hotel.AverageRating), rating);
        TestHelpers.SetPrivateProp(hotel, nameof(Hotel.ReviewCount), 8);

        var roomType = TestHelpers.CreateRoomType(name: $"{name}-type");
        var hrt = TestHelpers.CreateHotelRoomTypeFor(
            hotel,
            roomType,
            pricePerNight: minPrice,
            adultCapacity: adultCapacity,
            childCapacity: childCapacity);

        var room = new Room(Guid.NewGuid(), hrt.Id, hotel.Id, "101", 1, RoomStatus.Available);
        hrt.Rooms.Add(room);
        hotel.HotelRoomTypes.Add(hrt);

        var serviceLinks = new List<HotelService>();
        foreach (var amenity in amenities)
        {
            var service = new Service(Guid.NewGuid(), amenity, null);
            var link = new HotelService(Guid.NewGuid(), hotel.Id, service.Id, 0, true);
            TestHelpers.SetNav(link, nameof(HotelService.Service), service);
            serviceLinks.Add(link);
        }

        TestHelpers.SetNav(hotel, nameof(Hotel.HotelServices), serviceLinks);
        return (hotel, city, roomType);
    }

    [Fact]
    public async Task Handle_SqlFallback_RoomTypeFilter_Applies()
    {
        var h1 = BuildHotel("Hotel A", "Haifa", 5, 220m, 4.8);
        var h2 = BuildHotel("Hotel B", "Haifa", 4, 180m, 4.2);
        SetupDb([h1.hotel, h2.hotel]);

        var result = await Sut().Handle(new SearchHotelsQuery(
            Query: null,
            City: "haifa",
            RoomTypeId: h1.roomType.Id,
            CheckIn: null,
            CheckOut: null,
            Adults: null,
            Children: null,
            NumberOfRooms: null,
            MinPrice: null,
            MaxPrice: null,
            MinStarRating: null,
            Amenities: null,
            SortBy: "rating_desc",
            Cursor: null,
            Limit: 20), default);

        result.IsError.Should().BeFalse();
        result.Value.Items.Should().ContainSingle(i => i.HotelId == h1.hotel.Id);
    }

    [Fact]
    public async Task Handle_SqlFallback_OccupancyOnlyFilter_Applies()
    {
        var h1 = BuildHotel("Family Stay", "Nazareth", 4, 210m, 4.5, adultCapacity: 4, childCapacity: 2);
        var h2 = BuildHotel("Couple Inn", "Nazareth", 4, 170m, 4.0, adultCapacity: 2, childCapacity: 0);
        SetupDb([h1.hotel, h2.hotel]);

        var result = await Sut().Handle(new SearchHotelsQuery(
            Query: null,
            City: "nazareth",
            RoomTypeId: null,
            CheckIn: null,
            CheckOut: null,
            Adults: 3,
            Children: 1,
            NumberOfRooms: null,
            MinPrice: null,
            MaxPrice: null,
            MinStarRating: null,
            Amenities: null,
            SortBy: "rating_desc",
            Cursor: null,
            Limit: 20), default);

        result.IsError.Should().BeFalse();
        result.Value.Items.Should().ContainSingle(i => i.HotelId == h1.hotel.Id);
    }

    [Fact]
    public async Task Handle_SqlFallback_WhitespaceAmenities_AreIgnored()
    {
        var h1 = BuildHotel("Sea View", "Eilat", 5, 320m, 4.9, amenities: "Spa");
        var h2 = BuildHotel("City View", "Eilat", 4, 190m, 4.1, amenities: "Gym");
        SetupDb([h1.hotel, h2.hotel]);

        var result = await Sut().Handle(new SearchHotelsQuery(
            Query: null,
            City: "eilat",
            RoomTypeId: null,
            CheckIn: null,
            CheckOut: null,
            Adults: null,
            Children: null,
            NumberOfRooms: null,
            MinPrice: null,
            MaxPrice: null,
            MinStarRating: null,
            Amenities: [" ", "\t", ""],
            SortBy: "rating_desc",
            Cursor: null,
            Limit: 20), default);

        result.IsError.Should().BeFalse();
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_SqlFallback_PriceDesc_WithCursor_WorksAcrossPages()
    {
        var h1 = BuildHotel("Premium", "Jerusalem", 5, 400m, 4.6);
        var h2 = BuildHotel("Economy", "Jerusalem", 3, 120m, 4.0);
        SetupDb([h1.hotel, h2.hotel]);

        var first = await Sut().Handle(new SearchHotelsQuery(
            Query: null,
            City: "jerusalem",
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
            SortBy: "price_desc",
            Cursor: null,
            Limit: 1), default);

        first.IsError.Should().BeFalse();
        first.Value.Items.Should().ContainSingle();
        first.Value.Items[0].HotelId.Should().Be(h1.hotel.Id);
        first.Value.NextCursor.Should().NotBeNullOrWhiteSpace();

        var second = await Sut().Handle(new SearchHotelsQuery(
            Query: null,
            City: "jerusalem",
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
            SortBy: "price_desc",
            Cursor: first.Value.NextCursor,
            Limit: 1), default);

        second.IsError.Should().BeFalse();
        second.Value.Items.Should().ContainSingle();
        second.Value.Items[0].HotelId.Should().Be(h2.hotel.Id);
    }

    [Fact]
    public async Task Handle_SqlFallback_StarsDesc_WithCursor_WorksAcrossPages()
    {
        var h1 = BuildHotel("Five Star", "Akko", 5, 250m, 4.2);
        var h2 = BuildHotel("Four Star", "Akko", 4, 240m, 4.1);
        SetupDb([h1.hotel, h2.hotel]);

        var first = await Sut().Handle(new SearchHotelsQuery(
            Query: null,
            City: "akko",
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
            SortBy: "stars_desc",
            Cursor: null,
            Limit: 1), default);

        first.IsError.Should().BeFalse();
        first.Value.Items[0].HotelId.Should().Be(h1.hotel.Id);
        first.Value.NextCursor.Should().NotBeNullOrWhiteSpace();

        var second = await Sut().Handle(new SearchHotelsQuery(
            Query: null,
            City: "akko",
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
            SortBy: "stars_desc",
            Cursor: first.Value.NextCursor,
            Limit: 1), default);

        second.IsError.Should().BeFalse();
        second.Value.Items.Should().ContainSingle(i => i.HotelId == h2.hotel.Id);
    }

    [Fact]
    public async Task Handle_SqlFallback_RatingDesc_WithCursor_WorksAcrossPages()
    {
        var h1 = BuildHotel("Top Rated", "Tiberias", 4, 210m, 4.9);
        var h2 = BuildHotel("Good Rated", "Tiberias", 4, 200m, 4.1);
        SetupDb([h1.hotel, h2.hotel]);

        var first = await Sut().Handle(new SearchHotelsQuery(
            Query: null,
            City: "tiberias",
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
            SortBy: "rating_desc",
            Cursor: null,
            Limit: 1), default);

        first.IsError.Should().BeFalse();
        first.Value.Items[0].HotelId.Should().Be(h1.hotel.Id);
        first.Value.NextCursor.Should().NotBeNullOrWhiteSpace();

        var second = await Sut().Handle(new SearchHotelsQuery(
            Query: null,
            City: "tiberias",
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
            SortBy: "rating_desc",
            Cursor: first.Value.NextCursor,
            Limit: 1), default);

        second.IsError.Should().BeFalse();
        second.Value.Items.Should().ContainSingle(i => i.HotelId == h2.hotel.Id);
    }
}
