using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Common;
using HotelBooking.Domain.Hotels;

namespace HotelBooking.Domain.Bookings
{
    public class Booking : AuditableEntity
    {
        private Booking() { }

        public Booking(
            Guid id,
            string bookingNumber,
            Guid userId,
            Guid hotelId,
            string hotelName,
            string hotelAddress,
            string userEmail,
            DateOnly checkIn,
            DateOnly checkOut,
            decimal totalAmount,
            string? notes = null)
            : base(id)
        {
            BookingNumber = bookingNumber;
            UserId = userId;
            HotelId = hotelId;
            HotelName = hotelName;
            HotelAddress = hotelAddress;
            UserEmail = userEmail;
            CheckIn = checkIn;
            CheckOut = checkOut;
            TotalAmount = totalAmount;
            Notes = notes;
            Status = BookingStatus.Pending;
        }

        public string BookingNumber { get; private set; } = null!; // e.g. "BK-20260219-A7X3"
        public Guid UserId { get; private set; }
        public Guid HotelId { get; private set; }

        public string HotelName { get; private set; } = null!;
        public string HotelAddress { get; private set; } = null!;
        public string UserEmail { get; private set; } = null!;

        public DateOnly CheckIn { get; private set; }
        public DateOnly CheckOut { get; private set; }

        public decimal TotalAmount { get; private set; }

        public BookingStatus Status { get; private set; } = BookingStatus.Pending;

        public string? Notes { get; private set; }

        // Navigation
        public Hotel Hotel { get; private set; } = null!;
        public ICollection<BookingRoom> BookingRooms { get; private set; } = [];
        public ICollection<BookingService> BookingServices { get; private set; } = [];
        public ICollection<Payment> Payments { get; private set; } = [];
        public Cancellation? Cancellation { get; private set; }

        /// <summary>
        /// Normal transition after successful payment webhook / checkout completion.
        /// </summary>
        public void Confirm()
        {
            if (Status != BookingStatus.Pending)
                throw new InvalidOperationException($"Cannot confirm booking in {Status} status.");

            Status = BookingStatus.Confirmed;
        }

        /// <summary>
        /// Recovery transition for late payment success webhook after local timeout marked booking as Failed.
        /// Used by webhook reconciliation only.
        /// </summary>
        public void RecoverConfirmFromFailed()
        {
            if (Status != BookingStatus.Failed)
                throw new InvalidOperationException($"Cannot recover-confirm booking in {Status} status.");

            Status = BookingStatus.Confirmed;
        }

        public void MarkAsFailed()
        {
            if (Status != BookingStatus.Pending)
                throw new InvalidOperationException($"Cannot fail booking in {Status} status.");

            Status = BookingStatus.Failed;
        }

        public void CheckInGuest()
        {
            if (Status != BookingStatus.Confirmed)
                throw new InvalidOperationException($"Cannot check in booking in {Status} status.");

            Status = BookingStatus.CheckedIn;
        }

        public void Complete()
        {
            if (Status != BookingStatus.CheckedIn)
                throw new InvalidOperationException($"Cannot complete booking in {Status} status.");

            Status = BookingStatus.Completed;
        }

        public void Cancel()
        {
            if (Status != BookingStatus.Confirmed)
                throw new InvalidOperationException($"Cannot cancel booking in {Status} status.");

            Status = BookingStatus.Cancelled;
        }

        public void RecalculateTotal(decimal newTotal)
        {
            if (newTotal < 0)
                throw new ArgumentOutOfRangeException(nameof(newTotal), "Booking total cannot be negative.");

            TotalAmount = newTotal;
        }
    }
}