using FluentAssertions;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Features.Search.Queries.SearchHotels;
using HotelBooking.Application.Tests._Shared;
using HotelBooking.Contracts.Search;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Hotels.Enums;
using HotelBooking.Domain.Rooms;
using HotelBooking.Domain.Services;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace HotelBooking.Application.Tests.Search;

public sealed class SearchHotelsQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _db = new();
    private readonly Mock<IHotelSearchService> _search = new();
    private readonly Mock<ILogger<SearchHotelsQueryHandler>> _log = new();

    private SearchHotelsQueryHandler Sut() => new(_db.Object, _search.Object, _log.Object);

    [Fact]
    public async Task Handle_ElasticsearchAvailable_ReturnsElasticsearchResult()
    {
        _search.Setup(x => x.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _search.Setup(x => x.SearchAsync(It.IsAny<HotelSearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchHotelsResponse(new List<SearchHotelDto>(), null, false, 20));

        var result = await Sut().Handle(new SearchHotelsQuery(
            Query: "hotel", City: null, RoomTypeId: null, CheckIn: null, CheckOut: null,
            Adults: null, Children: null, NumberOfRooms: null, MinPrice: null, MaxPrice: null,
            MinStarRating: null, Amenities: null, SortBy: null, Cursor: null, Limit: 20), default);

        result.IsError.Should().BeFalse();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ElasticsearchFailure_FallsBackToSql()
    {
        var (hotel, city) = BuildHotel("Fallback Hotel", "Tiberias", 4, 220, 4.7);
        SetupDb(new List<Hotel> { hotel });

        _search.Setup(x => x.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _search.Setup(x => x.SearchAsync(It.IsAny<HotelSearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Result<SearchHotelsResponse>)Error.Failure("ES.Failed", "es failed"));

        var result = await Sut().Handle(new SearchHotelsQuery(
            Query: "fallback", City: null, RoomTypeId: null, CheckIn: null, CheckOut: null,
            Adults: null, Children: null, NumberOfRooms: null, MinPrice: null, MaxPrice: null,
            MinStarRating: null, Amenities: null, SortBy: "rating_desc", Cursor: null, Limit: 20), default);

        result.IsError.Should().BeFalse();
        result.Value.Items.Should().ContainSingle(i => i.HotelId == hotel.Id && i.CityName == city.Name);
    }

    [Fact]
    public async Task Handle_ElasticsearchThrows_FallsBackToSqlAndSupportsCursor()
    {
        var hotel1 = BuildHotel("A Hotel", "Haifa", 5, 100, 4.9).hotel;
        var hotel2 = BuildHotel("B Hotel", "Haifa", 4, 120, 4.2).hotel;
        SetupDb(new List<Hotel> { hotel1, hotel2 });

        _search.Setup(x => x.IsAvailableAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("down"));

        var firstPage = await Sut().Handle(new SearchHotelsQuery(
            Query: null, City: "haifa", RoomTypeId: null, CheckIn: null, CheckOut: null,
            Adults: null, Children: null, NumberOfRooms: null, MinPrice: null, MaxPrice: null,
            MinStarRating: null, Amenities: null, SortBy: "price_asc", Cursor: null, Limit: 1), default);

        firstPage.IsError.Should().BeFalse();
        firstPage.Value.HasMore.Should().BeTrue();
        firstPage.Value.NextCursor.Should().NotBeNullOrWhiteSpace();

        var secondPage = await Sut().Handle(new SearchHotelsQuery(
            Query: null, City: "haifa", RoomTypeId: null, CheckIn: null, CheckOut: null,
            Adults: null, Children: null, NumberOfRooms: null, MinPrice: null, MaxPrice: null,
            MinStarRating: null, Amenities: null, SortBy: "price_asc", Cursor: firstPage.Value.NextCursor, Limit: 1), default);

        secondPage.IsError.Should().BeFalse();
        secondPage.Value.Items.Should().ContainSingle();
        secondPage.Value.Items[0].HotelId.Should().NotBe(firstPage.Value.Items[0].HotelId);
    }

    [Fact]
    public async Task Handle_SqlFallback_FiltersByAmenitiesAndStars()
    {
        var hotel1 = BuildHotel("Spa Palace", "Jerusalem", 5, 350, 4.8, "Spa", "WiFi").hotel;
        var hotel2 = BuildHotel("Budget Inn", "Jerusalem", 3, 90, 3.9, "WiFi").hotel;
        SetupDb(new List<Hotel> { hotel1, hotel2 });

        _search.Setup(x => x.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await Sut().Handle(new SearchHotelsQuery(
            Query: null, City: "jerusalem", RoomTypeId: null, CheckIn: null, CheckOut: null,
            Adults: null, Children: null, NumberOfRooms: null, MinPrice: 100, MaxPrice: 500,
            MinStarRating: 4, Amenities: new List<string> { "spa" }, SortBy: "stars_desc", Cursor: null, Limit: 20), default);

        result.IsError.Should().BeFalse();
        result.Value.Items.Should().ContainSingle(i => i.HotelId == hotel1.Id);
    }

    [Fact]
    public async Task Handle_SqlFallback_InvalidCursor_IsIgnored()
    {
        var hotel = BuildHotel("Cursor Hotel", "Nazareth", 4, 180, 4.1).hotel;
        SetupDb(new List<Hotel> { hotel });
        _search.Setup(x => x.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await Sut().Handle(new SearchHotelsQuery(
            Query: "cursor", City: null, RoomTypeId: null, CheckIn: null, CheckOut: null,
            Adults: null, Children: null, NumberOfRooms: null, MinPrice: null, MaxPrice: null,
            MinStarRating: null, Amenities: null, SortBy: "rating_desc", Cursor: "not-base64", Limit: 20), default);

        result.IsError.Should().BeFalse();
        result.Value.Items.Should().ContainSingle(i => i.HotelId == hotel.Id);
    }

    private void SetupDb(List<Hotel> hotels)
    {
        _db.Setup(x => x.Hotels).Returns(hotels.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.BookingRooms).Returns(new List<BookingRoom>().AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.CheckoutHolds).Returns(new List<CheckoutHold>().AsQueryable().BuildMockDbSet().Object);
    }

    private static (Hotel hotel, City city) BuildHotel(
        string name,
        string cityName,
        short stars,
        decimal minPrice,
        double rating,
        params string[] amenities)
    {
        var city = TestHelpers.CreateCity(name: cityName, country: "IL");
        var hotel = TestHelpers.CreateHotel(cityId: city.Id, name: name, starRating: stars);
        TestHelpers.SetNav(hotel, nameof(Hotel.City), city);

        TestHelpers.SetPrivateProp(hotel, nameof(Hotel.MinPricePerNight), minPrice);
        TestHelpers.SetPrivateProp(hotel, nameof(Hotel.AverageRating), rating);
        TestHelpers.SetPrivateProp(hotel, nameof(Hotel.ReviewCount), 10);

        var serviceLinks = new List<HotelService>();
        foreach (var amenity in amenities)
        {
            var service = new Service(Guid.NewGuid(), amenity, null);
            var hs = new HotelService(Guid.NewGuid(), hotel.Id, service.Id, 0, true);
            TestHelpers.SetNav(hs, nameof(HotelService.Service), service);
            serviceLinks.Add(hs);
        }

        var roomType = TestHelpers.CreateRoomType(name: "Standard");
        var hrt = TestHelpers.CreateHotelRoomTypeFor(hotel, roomType, pricePerNight: minPrice);
        var room = new Room(Guid.NewGuid(), hrt.Id, hotel.Id, "101", 1, RoomStatus.Available);
        hrt.Rooms.Add(room);

        hotel.HotelRoomTypes.Add(hrt);
        TestHelpers.SetNav(hotel, nameof(Hotel.HotelServices), serviceLinks);

        return (hotel, city);
    }
}
