namespace HotelBooking.Application.Common.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Sends a booking confirmation email with invoice details.
    /// Fire-and-forget safe: implementations should not throw on transient failures.
    /// </summary>
    Task SendBookingConfirmationAsync(
        string toEmail,
        BookingConfirmationEmailData data,
        CancellationToken ct = default);
}

/// <summary>Data bag for the booking confirmation email template.</summary>
public sealed record BookingConfirmationEmailData(
    string BookingNumber,
    string HotelName,
    string HotelAddress,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Nights,
    decimal TotalAmount,
    string Currency,
    string TransactionRef,
    List<BookingRoomEmailItem> Rooms);

public sealed record BookingRoomEmailItem(
    string RoomTypeName,
    string RoomNumber,
    decimal PricePerNight,
    string Currency);