using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models.Payment;
using HotelBooking.Application.Settings;
using HotelBooking.Contracts.Checkout;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotelBooking.Application.Features.Checkout.Commands.CreateBooking;

public sealed class CreateBookingCommandHandler(
    IAppDbContext db,
    IPaymentGateway paymentGateway,
    IOptions<BookingSettings> bookingOptions,
    IOptions<PaymentUrlSettings> urlOptions,
    ILogger<CreateBookingCommandHandler> logger)
    : IRequestHandler<CreateBookingCommand, Result<CreateBookingResponse>>
{
    private readonly BookingSettings _booking = bookingOptions.Value;
    private readonly PaymentUrlSettings _urls = urlOptions.Value;

    public async Task<Result<CreateBookingResponse>> Handle(
        CreateBookingCommand cmd, CancellationToken ct)
    {
        // ── 1. Load holds ────────────────────────────────────────────────────
        var holds = await db.CheckoutHolds
            .Include(h => h.HotelRoomType)
                .ThenInclude(hrt => hrt.Hotel)
            .Include(h => h.HotelRoomType)
                .ThenInclude(hrt => hrt.RoomType)
            .Where(h => cmd.HoldIds.Contains(h.Id)
                     && h.UserId == cmd.UserId
                     && !h.IsReleased)
            .ToListAsync(ct);

        if (holds.Count == 0 || holds.Count != cmd.HoldIds.Count)
            return ApplicationErrors.Checkout.HoldExpired;

        if (holds.Any(h => h.IsExpired()))
            return ApplicationErrors.Checkout.HoldExpired;

        // ── 2. Derive booking metadata from holds ────────────────────────────
        var hotel = holds[0].HotelRoomType.Hotel;
        var checkIn = holds[0].CheckIn;
        var checkOut = holds[0].CheckOut;
        var nights = checkOut.DayNumber - checkIn.DayNumber;

        // ── 3. Assign specific rooms per hold ────────────────────────────────
        var bookingId = Guid.CreateVersion7();
        var bookingRooms = new List<BookingRoom>();

        foreach (var hold in holds)
        {
            // Rooms that are not in any active booking overlapping these dates
            var assignedRooms = await db.Rooms
                .Where(r => r.HotelRoomTypeId == hold.HotelRoomTypeId
                         && r.Status == "available"
                         && !db.BookingRooms.Any(br =>
                                br.RoomId == r.Id
                             && br.Booking.Status != BookingStatus.Cancelled
                             && br.Booking.Status != BookingStatus.Failed
                             && br.Booking.CheckIn < checkOut
                             && br.Booking.CheckOut > checkIn))
                .Take(hold.Quantity)
                .ToListAsync(ct);

            if (assignedRooms.Count < hold.Quantity)
            {
                logger.LogWarning(
                    "Room assignment failed for hold {HoldId}: needed {Needed}, found {Found}",
                    hold.Id, hold.Quantity, assignedRooms.Count);
                return ApplicationErrors.Payment.RoomNoLongerAvailable(
                    hold.HotelRoomType.RoomType.Name);
            }

            foreach (var room in assignedRooms)
            {
                bookingRooms.Add(new BookingRoom(
                    id: Guid.CreateVersion7(),
                    bookingId: bookingId,
                    hotelId: hotel.Id,
                    roomId: room.Id,
                    hotelRoomTypeId: hold.HotelRoomTypeId,
                    roomTypeName: hold.HotelRoomType.RoomType.Name,
                    roomNumber: room.RoomNumber,
                    pricePerNight: hold.HotelRoomType.PricePerNight));
            }
        }

        // ── 4. Calculate totals ──────────────────────────────────────────────
        var subtotal = holds.Sum(h => h.HotelRoomType.PricePerNight * nights * h.Quantity);
        var tax = subtotal * _booking.TaxRate;
        var total = subtotal + tax;

        // ── 5. Create Booking + Payment inside transaction ─────────────────────────
        var bookingNumber = GenerateBookingNumber();

        var booking = new Booking(
            id: bookingId,
            bookingNumber: bookingNumber,
            userId: cmd.UserId,
            hotelId: hotel.Id,
            hotelName: hotel.Name,
            hotelAddress: hotel.Address,
            checkIn: checkIn,
            checkOut: checkOut,
            totalAmount: total,
            notes: cmd.Notes);

        var payment = new Payment(
            id: Guid.CreateVersion7(),
            bookingId: bookingId,
            amount: total,
            method: PaymentMethod.Stripe);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        try
        {
            // أضف مرة واحدة فقط
            db.Bookings.Add(booking);
            db.BookingRooms.AddRange(bookingRooms);
            db.Payments.Add(payment);

            // الحفظ الأول: إنشاء سجل الحجز/الدفع بحالة Pending غالبًا
            await db.SaveChangesAsync(ct);

            // إنشاء جلسة الدفع
            PaymentSessionResponse session;
            try
            {
                session = await paymentGateway.CreatePaymentSessionAsync(
                    new PaymentSessionRequest(
                        BookingId: bookingId,
                        BookingNumber: bookingNumber,
                        AmountInUsd: total,
                        CustomerEmail: cmd.UserEmail,
                        HotelName: hotel.Name,
                        CheckIn: checkIn,
                        CheckOut: checkOut,
                        SuccessUrl: string.Format(_urls.SuccessUrlTemplate, bookingId),
                        CancelUrl: string.Format(_urls.CancelUrlTemplate, bookingId)),
                    ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to create {Provider} payment session for booking {BookingId}",
                    paymentGateway.ProviderName, bookingId);

                await tx.RollbackAsync(ct);
                return ApplicationErrors.Payment.GatewayUnavailable;
            }

            // تحديث Payment بالـ session id
            payment.SetProviderSession(session.SessionId);

            // الحفظ الثاني
            await db.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);

            logger.LogInformation(
                "Booking {BookingNumber} created. Payment session {SessionId} via {Provider}",
                bookingNumber, session.SessionId, paymentGateway.ProviderName);

            // إذا عندك implicit conversion من response إلى Result<response> فهذا يكفي
            return new CreateBookingResponse(
                BookingId: bookingId,
                BookingNumber: bookingNumber,
                TotalAmount: total,
                PaymentUrl: session.PaymentUrl,
                ExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(_booking.CheckoutHoldMinutes));

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create booking transaction for booking {BookingId}", bookingId);

            await tx.RollbackAsync(ct);
            throw;
        }

        // ── 9. Persist session ID ────────────────────────────────────────────
        payment.SetProviderSession(session.SessionId);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Booking {BookingNumber} created. Payment session {SessionId} via {Provider}",
            bookingNumber, session.SessionId, paymentGateway.ProviderName);

        return new CreateBookingResponse(
            BookingId: bookingId,
            BookingNumber: bookingNumber,
            TotalAmount: total,
            PaymentUrl: session.PaymentUrl,
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(_booking.CheckoutHoldMinutes));
    }

    private static string GenerateBookingNumber()
    {
        var datePart = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyyMMdd");
        var randomPart = Guid.CreateVersion7().ToString("N")[..6].ToUpperInvariant();
        return $"BK-{datePart}-{randomPart}";
    }
}