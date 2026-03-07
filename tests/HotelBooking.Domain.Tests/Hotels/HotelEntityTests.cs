/// <summary>
/// Tests for City entity — constructor and Update().
/// </summary>
using FluentAssertions;
using HotelBooking.Domain.Hotels;

namespace HotelBooking.Domain.Tests.Hotels;
using Xunit;
public class CityTests
{
    [Fact]
    public void Constructor_SetsAllFields()
    {
        var id = Guid.NewGuid();

        var city = new City(id, "Amman", "Jordan", "11180");

        city.Id.Should().Be(id);
        city.Name.Should().Be("Amman");
        city.Country.Should().Be("Jordan");
        city.PostOffice.Should().Be("11180");
    }

    [Fact]
    public void Constructor_NullPostOffice_Allowed()
    {
        var city = new City(Guid.NewGuid(), "Dubai", "UAE", null);

        city.PostOffice.Should().BeNull();
    }

    [Fact]
    public void Update_ChangesAllFields()
    {
        var city = new City(Guid.NewGuid(), "Old Name", "Old Country", "00000");

        city.Update("New Name", "New Country", "99999");

        city.Name.Should().Be("New Name");
        city.Country.Should().Be("New Country");
        city.PostOffice.Should().Be("99999");
    }
}


/// <summary>
/// Tests for Hotel entity — constructor, Update, UpdatePriceSummary,
/// UpdateReviewSummary, and SetThumbnail.
/// </summary>
public class HotelTests
{
    private static Hotel CreateHotel()
        => new(
            id: Guid.NewGuid(),
            cityId: Guid.NewGuid(),
            name: "Grand Palace",
            owner: "John Doe",
            address: "1 King St",
            starRating: 5);

    [Fact]
    public void Constructor_SetsAllFields()
    {
        var id = Guid.NewGuid();
        var cityId = Guid.NewGuid();

        var hotel = new Hotel(id, cityId, "Test Hotel", "Owner", "123 St", 4,
            "Lovely place", 31.5m, 36.2m, "15:00", "12:00");

        hotel.Id.Should().Be(id);
        hotel.CityId.Should().Be(cityId);
        hotel.Name.Should().Be("Test Hotel");
        hotel.Owner.Should().Be("Owner");
        hotel.Address.Should().Be("123 St");
        hotel.StarRating.Should().Be(4);
        hotel.Description.Should().Be("Lovely place");
        hotel.Latitude.Should().Be(31.5m);
        hotel.Longitude.Should().Be(36.2m);
        hotel.CheckInTime.Should().Be("15:00");
        hotel.CheckOutTime.Should().Be("12:00");
    }

    [Fact]
    public void Update_ChangesAllFields()
    {
        var hotel = CreateHotel();

        hotel.Update("New Name", "New Owner", "New Addr", 3, "Desc", 10m, 20m, "13:00", "10:00");

        hotel.Name.Should().Be("New Name");
        hotel.Owner.Should().Be("New Owner");
        hotel.Address.Should().Be("New Addr");
        hotel.StarRating.Should().Be(3);
        hotel.Description.Should().Be("Desc");
        hotel.Latitude.Should().Be(10m);
        hotel.Longitude.Should().Be(20m);
        hotel.CheckInTime.Should().Be("13:00");
        hotel.CheckOutTime.Should().Be("10:00");
    }

    [Fact]
    public void UpdatePriceSummary_SetsMinPrice()
    {
        var hotel = CreateHotel();

        hotel.UpdatePriceSummary(120m);

        hotel.MinPricePerNight.Should().Be(120m);
    }

    [Fact]
    public void UpdateReviewSummary_SetsAverageAndCount()
    {
        var hotel = CreateHotel();

        hotel.UpdateReviewSummary(4.5, 200);

        hotel.AverageRating.Should().Be(4.5);
        hotel.ReviewCount.Should().Be(200);
    }

    [Fact]
    public void SetThumbnail_SetsUrl()
    {
        var hotel = CreateHotel();

        hotel.SetThumbnail("https://cdn.example.com/hotel.jpg");

        hotel.ThumbnailUrl.Should().Be("https://cdn.example.com/hotel.jpg");
    }

    [Fact]
    public void SetThumbnail_Null_ClearsUrl()
    {
        var hotel = CreateHotel();
        hotel.SetThumbnail("https://cdn.example.com/hotel.jpg");

        hotel.SetThumbnail(null);

        hotel.ThumbnailUrl.Should().BeNull();
    }
}


/// <summary>
/// Tests for FeaturedDeal entity — IsActive() date logic and constructor.
/// </summary>
public class FeaturedDealTests
{
    [Fact]
    public void Constructor_SetsAllFields()
    {
        var id = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var hotelRoomTypeId = Guid.NewGuid();
        var starts = DateTimeOffset.UtcNow.AddDays(-1);
        var ends = DateTimeOffset.UtcNow.AddDays(7);

        var deal = new FeaturedDeal(id, hotelId, hotelRoomTypeId, 300m, 200m, 1, starts, ends);

        deal.Id.Should().Be(id);
        deal.HotelId.Should().Be(hotelId);
        deal.HotelRoomTypeId.Should().Be(hotelRoomTypeId);
        deal.OriginalPrice.Should().Be(300m);
        deal.DiscountedPrice.Should().Be(200m);
        deal.DisplayOrder.Should().Be(1);
        deal.StartsAtUtc.Should().Be(starts);
        deal.EndsAtUtc.Should().Be(ends);
    }

    [Fact]
    public void IsActive_NullDates_ReturnsTrue()
    {
        var deal = new FeaturedDeal(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            300m, 200m, 0, null, null);

        deal.IsActive().Should().BeTrue();
    }

    [Fact]
    public void IsActive_CurrentlyActive_ReturnsTrue()
    {
        var deal = new FeaturedDeal(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            300m, 200m, 0,
            startsAtUtc: DateTimeOffset.UtcNow.AddDays(-1),
            endsAtUtc: DateTimeOffset.UtcNow.AddDays(1));

        deal.IsActive().Should().BeTrue();
    }

    [Fact]
    public void IsActive_Expired_ReturnsFalse()
    {
        var deal = new FeaturedDeal(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            300m, 200m, 0,
            startsAtUtc: DateTimeOffset.UtcNow.AddDays(-10),
            endsAtUtc: DateTimeOffset.UtcNow.AddDays(-1));

        deal.IsActive().Should().BeFalse();
    }

    [Fact]
    public void IsActive_Future_ReturnsFalse()
    {
        var deal = new FeaturedDeal(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            300m, 200m, 0,
            startsAtUtc: DateTimeOffset.UtcNow.AddDays(5),
            endsAtUtc: DateTimeOffset.UtcNow.AddDays(10));

        deal.IsActive().Should().BeFalse();
    }

    [Fact]
    public void Update_ChangesFields()
    {
        var deal = new FeaturedDeal(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            300m, 200m, 0, null, null);
        var newStart = DateTimeOffset.UtcNow.AddDays(1);
        var newEnd = DateTimeOffset.UtcNow.AddDays(10);

        deal.Update(400m, 250m, 2, newStart, newEnd);

        deal.OriginalPrice.Should().Be(400m);
        deal.DiscountedPrice.Should().Be(250m);
        deal.DisplayOrder.Should().Be(2);
        deal.StartsAtUtc.Should().Be(newStart);
        deal.EndsAtUtc.Should().Be(newEnd);
    }
}
