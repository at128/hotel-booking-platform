using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Checkout;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Checkout.Queries.GetBooking;

public sealed class GetBookingQueryHandler(IAppDbContext db)
    : IRequestHandler<GetBookingQuery, Result<BookingDetailsResponse>>
{
    public async Task<Result<BookingDetailsResponse>> Handle(
        GetBookingQuery query, CancellationToken ct)
    {
        var booking = await db.Bookings
            .AsNoTracking()
            .Include(b => b.BookingRooms)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == query.BookingId, ct);

        if (booking is null)
            return ApplicationErrors.Booking.NotFound;

        // Authorization: owner OR admin
        if (!query.IsAdmin && booking.UserId != query.RequestingUserId)
            return ApplicationErrors.Booking.AccessDenied;

        var nights = booking.CheckOut.DayNumber - booking.CheckIn.DayNumber;

        var rooms = booking.BookingRooms
            .Select(r => new BookingRoomDto(r.RoomTypeName, r.RoomNumber, r.PricePerNight))
            .ToList();

        // Most recent payment (usually only one, but take the latest)
        var payment = booking.Payments
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new BookingPaymentDto(
                p.Status.ToString(),
                p.TransactionRef,
                p.PaidAtUtc))
            .FirstOrDefault();

        return new BookingDetailsResponse(
            BookingId: booking.Id,
            BookingNumber: booking.BookingNumber,
            HotelName: booking.HotelName,
            HotelAddress: booking.HotelAddress,
            CheckIn: booking.CheckIn,
            CheckOut: booking.CheckOut,
            Nights: nights,
            TotalAmount: booking.TotalAmount,
            Status: booking.Status.ToString(),
            Notes: booking.Notes,
            Rooms: rooms,
            Payment: payment);
    }
}