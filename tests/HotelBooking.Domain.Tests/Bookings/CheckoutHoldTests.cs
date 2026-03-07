/// <summary>
/// Tests for the CheckoutHold entity — expiry logic and release.
/// </summary>
using FluentAssertions;
using HotelBooking.Domain.Bookings;
using Xunit;

namespace HotelBooking.Domain.Tests.Bookings;

public class CheckoutHoldTests
{
    private static CheckoutHold CreateHold(DateTimeOffset expiresAt)
        => new(
            id: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            hotelId: Guid.NewGuid(),
            hotelRoomTypeId: Guid.NewGuid(),
            checkIn: new DateOnly(2026, 6, 1),
            checkOut: new DateOnly(2026, 6, 5),
            quantity: 1,
            expiresAtUtc: expiresAt);

    [Fact]
    public void IsExpired_PastExpiry_True()
    {
        var hold = CreateHold(DateTimeOffset.UtcNow.AddMinutes(-1));

        hold.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsExpired_FutureExpiry_False()
    {
        var hold = CreateHold(DateTimeOffset.UtcNow.AddMinutes(10));

        hold.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void Release_SetsIsReleasedTrue()
    {
        var hold = CreateHold(DateTimeOffset.UtcNow.AddMinutes(10));

        hold.Release();

        hold.IsReleased.Should().BeTrue();
    }
}
