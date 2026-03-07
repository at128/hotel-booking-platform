/// <summary>
/// Tests for the Booking aggregate root — covers all state machine transitions
/// and the RecalculateTotal business rule.
/// </summary>
using FluentAssertions;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Bookings.Enums;
using Xunit;

namespace HotelBooking.Domain.Tests.Bookings;

public class BookingTests
{
    #region Helpers

    private static Booking CreateBooking(BookingStatus? overrideStatus = null)
    {
        var booking = new Booking(
            id: Guid.NewGuid(),
            bookingNumber: "BK-20260101-TEST",
            userId: Guid.NewGuid(),
            hotelId: Guid.NewGuid(),
            hotelName: "Test Hotel",
            hotelAddress: "123 Main St",
            userEmail: "guest@example.com",
            checkIn: new DateOnly(2026, 6, 1),
            checkOut: new DateOnly(2026, 6, 5),
            totalAmount: 500m);

        if (overrideStatus is not null)
            ForceStatus(booking, overrideStatus.Value);

        return booking;
    }

    /// <summary>Force a booking into a specific status via valid state transitions.</summary>
    private static void ForceStatus(Booking booking, BookingStatus target)
    {
        switch (target)
        {
            case BookingStatus.Confirmed:
                booking.Confirm();
                break;
            case BookingStatus.CheckedIn:
                booking.Confirm();
                booking.CheckInGuest();
                break;
            case BookingStatus.Completed:
                booking.Confirm();
                booking.CheckInGuest();
                booking.Complete();
                break;
            case BookingStatus.Cancelled:
                booking.Confirm();
                booking.Cancel();
                break;
            case BookingStatus.Failed:
                booking.MarkAsFailed();
                break;
        }
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_ShouldSetAllFields()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var checkIn = new DateOnly(2026, 6, 1);
        var checkOut = new DateOnly(2026, 6, 5);

        // Act
        var booking = new Booking(id, "BK-001", userId, hotelId,
            "Grand Hotel", "456 Park Ave", "user@test.com",
            checkIn, checkOut, 800m, "Special request");

        // Assert
        booking.Id.Should().Be(id);
        booking.BookingNumber.Should().Be("BK-001");
        booking.UserId.Should().Be(userId);
        booking.HotelId.Should().Be(hotelId);
        booking.HotelName.Should().Be("Grand Hotel");
        booking.HotelAddress.Should().Be("456 Park Ave");
        booking.UserEmail.Should().Be("user@test.com");
        booking.CheckIn.Should().Be(checkIn);
        booking.CheckOut.Should().Be(checkOut);
        booking.TotalAmount.Should().Be(800m);
        booking.Notes.Should().Be("Special request");
    }

    [Fact]
    public void Constructor_ShouldDefaultStatusToPending()
    {
        var booking = CreateBooking();

        booking.Status.Should().Be(BookingStatus.Pending);
    }

    #endregion

    #region Confirm

    [Fact]
    public void Confirm_WhenPending_SetsConfirmed()
    {
        var booking = CreateBooking();

        booking.Confirm();

        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Failed)]
    [InlineData(BookingStatus.CheckedIn)]
    [InlineData(BookingStatus.Completed)]
    public void Confirm_WhenNotPending_Throws(BookingStatus startStatus)
    {
        var booking = CreateBooking(startStatus);

        var act = () => booking.Confirm();

        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region MarkAsFailed

    [Fact]
    public void MarkAsFailed_WhenPending_SetsFailed()
    {
        var booking = CreateBooking();

        booking.MarkAsFailed();

        booking.Status.Should().Be(BookingStatus.Failed);
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Failed)]
    [InlineData(BookingStatus.CheckedIn)]
    [InlineData(BookingStatus.Completed)]
    public void MarkAsFailed_WhenNotPending_Throws(BookingStatus startStatus)
    {
        var booking = CreateBooking(startStatus);

        var act = () => booking.MarkAsFailed();

        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region CheckInGuest

    [Fact]
    public void CheckInGuest_WhenConfirmed_SetsCheckedIn()
    {
        var booking = CreateBooking(BookingStatus.Confirmed);

        booking.CheckInGuest();

        booking.Status.Should().Be(BookingStatus.CheckedIn);
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Failed)]
    [InlineData(BookingStatus.CheckedIn)]
    [InlineData(BookingStatus.Completed)]
    public void CheckInGuest_WhenNotConfirmed_Throws(BookingStatus startStatus)
    {
        var booking = CreateBooking(startStatus);

        var act = () => booking.CheckInGuest();

        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region Complete

    [Fact]
    public void Complete_WhenCheckedIn_SetsCompleted()
    {
        var booking = CreateBooking(BookingStatus.CheckedIn);

        booking.Complete();

        booking.Status.Should().Be(BookingStatus.Completed);
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Failed)]
    [InlineData(BookingStatus.Completed)]
    public void Complete_WhenNotCheckedIn_Throws(BookingStatus startStatus)
    {
        var booking = CreateBooking(startStatus);

        var act = () => booking.Complete();

        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region Cancel

    [Fact]
    public void Cancel_WhenConfirmed_SetsCancelled()
    {
        var booking = CreateBooking(BookingStatus.Confirmed);

        booking.Cancel();

        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Failed)]
    [InlineData(BookingStatus.CheckedIn)]
    [InlineData(BookingStatus.Completed)]
    public void Cancel_WhenNotConfirmed_Throws(BookingStatus startStatus)
    {
        var booking = CreateBooking(startStatus);

        var act = () => booking.Cancel();

        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region RecoverConfirmFromFailed

    [Fact]
    public void RecoverConfirmFromFailed_WhenFailed_SetsConfirmed()
    {
        var booking = CreateBooking(BookingStatus.Failed);

        booking.RecoverConfirmFromFailed();

        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.CheckedIn)]
    [InlineData(BookingStatus.Completed)]
    public void RecoverConfirmFromFailed_WhenNotFailed_Throws(BookingStatus startStatus)
    {
        var booking = CreateBooking(startStatus);

        var act = () => booking.RecoverConfirmFromFailed();

        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region RecalculateTotal

    [Fact]
    public void RecalculateTotal_ValidAmount_Updates()
    {
        var booking = CreateBooking();

        booking.RecalculateTotal(750m);

        booking.TotalAmount.Should().Be(750m);
    }

    [Fact]
    public void RecalculateTotal_NegativeAmount_ThrowsArgumentOutOfRange()
    {
        var booking = CreateBooking();

        var act = () => booking.RecalculateTotal(-1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RecalculateTotal_ZeroAmount_Succeeds()
    {
        var booking = CreateBooking();

        booking.RecalculateTotal(0m);

        booking.TotalAmount.Should().Be(0m);
    }

    #endregion
}
