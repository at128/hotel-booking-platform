/// <summary>
/// Tests for CartItem and HotelVisit domain entities.
/// </summary>
using FluentAssertions;
using HotelBooking.Domain.Cart;
using HotelBooking.Domain.Hotels;
using Xunit;
namespace HotelBooking.Domain.Tests.Hotels;

public class CartItemTests
{
    [Fact]
    public void Constructor_SetsAllFields()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var hotelRoomTypeId = Guid.NewGuid();
        var checkIn = new DateOnly(2026, 6, 1);
        var checkOut = new DateOnly(2026, 6, 5);

        var item = new CartItem(id, userId, hotelId, hotelRoomTypeId, checkIn, checkOut, 2);

        item.Id.Should().Be(id);
        item.UserId.Should().Be(userId);
        item.HotelId.Should().Be(hotelId);
        item.HotelRoomTypeId.Should().Be(hotelRoomTypeId);
        item.CheckIn.Should().Be(checkIn);
        item.CheckOut.Should().Be(checkOut);
        item.Quantity.Should().Be(2);
    }

    [Fact]
    public void UpdateQuantity_ChangesQuantity()
    {
        var item = new CartItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 5), 1);

        item.UpdateQuantity(3);

        item.Quantity.Should().Be(3);
    }
}

public class HotelVisitTests
{
    [Fact]
    public void Constructor_SetsAllFields()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();

        var visit = new HotelVisit(id, userId, hotelId);

        visit.Id.Should().Be(id);
        visit.UserId.Should().Be(userId);
        visit.HotelId.Should().Be(hotelId);
    }

    [Fact]
    public void UpdateVisitTime_UpdatesTimestamp()
    {
        var visit = new HotelVisit(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var before = DateTimeOffset.UtcNow;

        visit.UpdateVisitTime();

        visit.VisitedAtUtc.Should().BeOnOrAfter(before);
    }
}
