using System.Data;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models.Payment;
using HotelBooking.Application.Settings;
using HotelBooking.Contracts.Checkout;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Rooms;
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
        // Phase A: DB-only transaction (create booking/payment + assign rooms + release holds)
        var phaseA = await CreatePendingBookingAsync(cmd, ct);
        if (phaseA.Error is not null)
            return phaseA.Error;

        var created = phaseA.Value!;

        // Phase B: External payment provider call (OUTSIDE DB transaction)
        PaymentSessionResponse session;
        try
        {
            session = await paymentGateway.CreatePaymentSessionAsync(
                new PaymentSessionRequest(
                    BookingId: created.BookingId,
                    BookingNumber: created.BookingNumber,
                    AmountInUsd: created.TotalAmount,
                    CustomerEmail: cmd.UserEmail,
                    HotelName: created.HotelName,
                    CheckIn: created.CheckIn,
                    CheckOut: created.CheckOut,
                    SuccessUrl: string.Format(_urls.SuccessUrlTemplate, created.BookingId),
                    CancelUrl: string.Format(_urls.CancelUrlTemplate, created.BookingId)),
                ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to create {Provider} payment session for booking {BookingId}",
                paymentGateway.ProviderName, created.BookingId);

            await MarkPaymentInitiationFailedSafeAsync(created.PaymentId, ct);

            return ApplicationErrors.Payment.GatewayUnavailable;
        }

        // Phase C: Persist provider session id in a short DB transaction
        try
        {
            // Phase C: Persist provider session id in a short DB transaction
            await PersistPaymentSessionAsync(created.PaymentId, session.SessionId, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Phase C failed: could not persist provider session linkage. BookingId={BookingId}, PaymentId={PaymentId}, SessionId={SessionId}, Provider={Provider}",
                created.BookingId,
                created.PaymentId,
                session.SessionId,
                paymentGateway.ProviderName);

            try
            {
                await paymentGateway.ExpirePaymentSessionAsync(session.SessionId, ct);

                logger.LogWarning(
                    "Compensation succeeded: provider session {SessionId} expired after Phase C failure for payment {PaymentId}",
                    session.SessionId,
                    created.PaymentId);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception compensationEx)
            {
                logger.LogCritical(
                    compensationEx,
                    "Compensation failed: provider session {SessionId} remains active after Phase C failure. BookingId={BookingId}, PaymentId={PaymentId}",
                    session.SessionId,
                    created.BookingId,
                    created.PaymentId);
            }

            await MarkPaymentInitiationFailedSafeAsync(created.PaymentId, ct);

            return ApplicationErrors.Payment.GatewayUnavailable;
        }

        logger.LogInformation(
            "Booking {BookingNumber} created. Payment session {SessionId} via {Provider}",
            created.BookingNumber, session.SessionId, paymentGateway.ProviderName);

        return new CreateBookingResponse(
            BookingId: created.BookingId,
            BookingNumber: created.BookingNumber,
            TotalAmount: created.TotalAmount,
            PaymentUrl: session.PaymentUrl,
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(_booking.CheckoutHoldMinutes));
    }

    private async Task<PhaseAResult> CreatePendingBookingAsync(
        CreateBookingCommand cmd,
        CancellationToken ct)
    {
        await using var tx = await db.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            // 1) Load active holds for this user
            var requestedHoldIds = cmd.HoldIds.Distinct().ToList();

            var holds = await db.CheckoutHolds
                .Include(h => h.HotelRoomType)
                    .ThenInclude(hrt => hrt.Hotel)
                .Include(h => h.HotelRoomType)
                    .ThenInclude(hrt => hrt.RoomType)
                .Where(h => requestedHoldIds.Contains(h.Id)
                         && h.UserId == cmd.UserId
                         && !h.IsReleased)
                .ToListAsync(ct);

            if (holds.Count == 0 || holds.Count != requestedHoldIds.Count)
                return PhaseAResult.Fail(ApplicationErrors.Checkout.HoldExpired);

            if (holds.Any(h => h.IsExpired()))
                return PhaseAResult.Fail(ApplicationErrors.Checkout.HoldExpired);

            var holdsValidationError = ValidateHoldsConsistency(holds);
            if (holdsValidationError is {} holdError)
                return PhaseAResult.Fail(holdError);

            // 2) Derive booking metadata from holds
            var firstHold = holds[0];
            var hotel = firstHold.HotelRoomType.Hotel;
            var checkIn = firstHold.CheckIn;
            var checkOut = firstHold.CheckOut;
            var nights = checkOut.DayNumber - checkIn.DayNumber;

            if (nights <= 0)
                return PhaseAResult.Fail(ApplicationErrors.Cart.InvalidDates);

            // 3) Assign specific rooms (inside transaction)
            var bookingId = Guid.CreateVersion7();
            var bookingRooms = await AssignRoomsAsync(
                bookingId: bookingId,
                holds: holds,
                checkIn: checkIn,
                checkOut: checkOut,
                ct: ct);

            if (bookingRooms is null)
            {
                logger.LogWarning(
                    "Room assignment failed during CreateBooking for user {UserId}. Holds: {HoldIds}",
                    cmd.UserId, requestedHoldIds);

                return PhaseAResult.Fail(ApplicationErrors.Payment.RoomNoLongerAvailable(
                    firstHold.HotelRoomType.RoomType.Name));
            }

            // 4) Calculate totals
            var rawSubtotal = holds.Sum(h => h.HotelRoomType.PricePerNight * nights * h.Quantity);
            var subtotal = RoundMoney(rawSubtotal);

            var rawTax = subtotal * _booking.TaxRate;
            var tax = RoundMoney(rawTax);

            var total = RoundMoney(subtotal + tax);

            // 5) Create booking + payment
            var bookingNumber = GenerateBookingNumber();

            var booking = new Booking(
                id: bookingId,
                bookingNumber: bookingNumber,
                userId: cmd.UserId,
                hotelId: hotel.Id,
                hotelName: hotel.Name,
                hotelAddress: hotel.Address,
                userEmail: cmd.UserEmail,
                checkIn: checkIn,
                checkOut: checkOut,
                totalAmount: total,
                notes: cmd.Notes);

            var payment = new Payment(
                id: Guid.CreateVersion7(),
                bookingId: bookingId,
                amount: total,
                method: PaymentMethod.Stripe);

            db.Bookings.Add(booking);
            db.BookingRooms.AddRange(bookingRooms);
            db.Payments.Add(payment);

            foreach (var hold in holds)
            {
                hold.Release();
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return PhaseAResult.Ok(new CreatedBookingSnapshot(
                BookingId: bookingId,
                PaymentId: payment.Id,
                BookingNumber: bookingNumber,
                HotelName: hotel.Name,
                CheckIn: checkIn,
                CheckOut: checkOut,
                TotalAmount: total));
        }
        catch (OperationCanceledException)
        {
            try
            {
                await tx.RollbackAsync(CancellationToken.None);
            }
            catch
            {

            }

            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create booking transaction for user {UserId}", cmd.UserId);

            try
            {
                await tx.RollbackAsync(ct);
            }
            catch (Exception rollbackEx)
            {
                logger.LogWarning(rollbackEx,
                    "Rollback failed while creating booking for user {UserId}", cmd.UserId);
            }

            throw;
        }
    }

    private async Task<List<BookingRoom>?> AssignRoomsAsync(
        Guid bookingId,
        List<CheckoutHold> holds,
        DateOnly checkIn,
        DateOnly checkOut,
        CancellationToken ct)
    {
        var bookingRooms = new List<BookingRoom>();

        var alreadyAssignedRoomIds = new HashSet<Guid>();

        foreach (var group in holds.GroupBy(h => h.HotelRoomTypeId))
        {
            var sampleHold = group.First();
            var requiredQuantity = group.Sum(h => h.Quantity);

            var assignedRooms = await db.Rooms
                .Where(r => r.HotelRoomTypeId == group.Key
                         && r.Status == RoomStatus.Available
                         && !alreadyAssignedRoomIds.Contains(r.Id)
                         && !db.BookingRooms.Any(br =>
                                br.RoomId == r.Id
                             && br.Booking.Status != BookingStatus.Cancelled
                             && br.Booking.Status != BookingStatus.Failed
                             && br.Booking.CheckIn < checkOut
                             && br.Booking.CheckOut > checkIn))
                .Take(requiredQuantity)
                .ToListAsync(ct);

            if (assignedRooms.Count < requiredQuantity)
            {
                logger.LogWarning(
                    "Room assignment failed for room type {RoomTypeId}: needed {Needed}, found {Found}",
                    group.Key, requiredQuantity, assignedRooms.Count);

                return null;
            }

            foreach (var room in assignedRooms)
            {
                alreadyAssignedRoomIds.Add(room.Id);

                bookingRooms.Add(new BookingRoom(
                    id: Guid.CreateVersion7(),
                    bookingId: bookingId,
                    hotelId: sampleHold.HotelRoomType.Hotel.Id,
                    roomId: room.Id,
                    hotelRoomTypeId: sampleHold.HotelRoomTypeId,
                    roomTypeName: sampleHold.HotelRoomType.RoomType.Name,
                    roomNumber: room.RoomNumber,
                    pricePerNight: sampleHold.HotelRoomType.PricePerNight));
            }
        }

        return bookingRooms;
    }

    private static Error? ValidateHoldsConsistency(List<CheckoutHold> holds)
    {
        if (holds.Count == 0)
            return ApplicationErrors.Checkout.HoldExpired;

        var first = holds[0];

        // كل الـholds لازم تكون لنفس الفندق ونفس الفترة
        if (holds.Any(h => h.HotelRoomType.HotelId != first.HotelRoomType.HotelId))
            return ApplicationErrors.Checkout.HoldExpired;

        if (holds.Any(h => h.CheckIn != first.CheckIn || h.CheckOut != first.CheckOut))
            return ApplicationErrors.Checkout.HoldExpired;

        return null;
    }

    private async Task PersistPaymentSessionAsync(Guid paymentId, string sessionId, CancellationToken ct)
    {
        await using var tx = await db.BeginTransactionAsync(ct);

        var payment = await db.Payments
            .FirstOrDefaultAsync(p => p.Id == paymentId, ct);

        if (payment is null)
            throw new InvalidOperationException($"Payment {paymentId} was not found after booking creation.");

        payment.SetProviderSession(sessionId);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private static string GenerateBookingNumber()
    {
        var datePart = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyyMMdd");
        var randomPart = Guid.CreateVersion7().ToString("N")[..6].ToUpperInvariant();
        return $"BK-{datePart}-{randomPart}";
    }

    private sealed record CreatedBookingSnapshot(
        Guid BookingId,
        Guid PaymentId,
        string BookingNumber,
        string HotelName,
        DateOnly CheckIn,
        DateOnly CheckOut,
        decimal TotalAmount);

    private sealed class PhaseAResult
    {
        private PhaseAResult(Error? error, CreatedBookingSnapshot? value)
        {
            Error = error;
            Value = value;
        }

        public Error? Error { get; }
        public CreatedBookingSnapshot? Value { get; }

        public static PhaseAResult Ok(CreatedBookingSnapshot value) => new(null, value);
        public static PhaseAResult Fail(Error error) => new(error, null);
    }

    private async Task MarkPaymentInitiationFailedSafeAsync(Guid paymentId, CancellationToken ct)
    {
        try
        {
            await using var tx = await db.BeginTransactionAsync(ct);

            var payment = await db.Payments
                .FirstOrDefaultAsync(p => p.Id == paymentId, ct);

            if (payment is null)
                return;

            payment.MarkInitiationFailed();

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to mark payment {PaymentId} as initiation-failed after gateway error",
                paymentId);
        }
    }

    private static decimal RoundMoney(decimal amount)
    => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}