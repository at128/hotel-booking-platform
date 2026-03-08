/// <summary>
/// Tests for the Cancellation entity — refund status transitions.
/// </summary>
using FluentAssertions;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Bookings.Enums;
using Xunit;

namespace HotelBooking.Domain.Tests.Bookings;

public class CancellationTests
{
    private static Cancellation CreateCancellation()
        => new(
            id: Guid.NewGuid(),
            bookingId: Guid.NewGuid(),
            reason: "Guest changed plans",
            refundAmount: 350m,
            refundPercentage: 0.70m);

    [Fact]
    public void MarkRefundProcessed_WhenPending_Succeeds()
    {
        var cancellation = CreateCancellation();

        cancellation.MarkRefundProcessed();

        cancellation.RefundStatus.Should().Be(RefundStatus.Processed);
    }

    [Fact]
    public void MarkRefundProcessed_WhenNotPending_Throws()
    {
        var cancellation = CreateCancellation();
        cancellation.MarkRefundProcessed();

        var act = () => cancellation.MarkRefundProcessed();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkRefundFailed_WhenPending_Succeeds()
    {
        var cancellation = CreateCancellation();

        cancellation.MarkRefundFailed();

        cancellation.RefundStatus.Should().Be(RefundStatus.Failed);
    }

    [Fact]
    public void MarkRefundFailed_WhenNotPending_Throws()
    {
        var cancellation = CreateCancellation();
        cancellation.MarkRefundFailed();

        var act = () => cancellation.MarkRefundFailed();

        act.Should().Throw<InvalidOperationException>();
    }
}
