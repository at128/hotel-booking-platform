using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Domain.Bookings
{
    public class Cancellation : Entity
    {
        private Cancellation() { }

        public Cancellation(
            Guid id,
            Guid bookingId,
            string? reason,
            decimal refundAmount,
            decimal refundPercentage)
            : base(id)
        {
            BookingId = bookingId;
            Reason = reason;
            RefundAmount = refundAmount;
            RefundPercentage = refundPercentage;
            CancelledAtUtc = DateTimeOffset.UtcNow;
        }

        public Guid BookingId { get; private set; } // UNIQUE — one cancellation per booking
        public string? Reason { get; private set; }
        public decimal RefundAmount { get; private set; }
        public decimal RefundPercentage { get; private set; } // 100% or 70% per policy
        public RefundStatus RefundStatus { get; private set; } = RefundStatus.Pending;
        public DateTimeOffset CancelledAtUtc { get; private set; }

        public Booking Booking { get; private set; } = null!;

        public void MarkRefundProcessed()
        {
            if (RefundStatus != RefundStatus.Pending)
                throw new InvalidOperationException($"Cannot process refund in {RefundStatus} status.");
            RefundStatus = RefundStatus.Processed;
        }

        public void MarkRefundFailed()
        {
            if (RefundStatus != RefundStatus.Pending)
                throw new InvalidOperationException($"Cannot fail refund in {RefundStatus} status.");
            RefundStatus = RefundStatus.Failed;
        }
    }

}
