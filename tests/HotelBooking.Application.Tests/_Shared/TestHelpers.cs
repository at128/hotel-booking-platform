using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models.Payment;
using HotelBooking.Application.Settings;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Cart;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;
using HotelBooking.Domain.Services;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using MockQueryable.Moq;
using Moq;
using System.Data;
using System.Reflection;
using Xunit;
namespace HotelBooking.Application.Tests._Shared;

/// <summary>
/// Shared factory helpers for ALL test projects.
/// Creates valid domain entities using public constructors.
/// Uses reflection only for navigation properties (private setters).
/// </summary>
public static class TestHelpers
{
    // ─── Settings Factories ──────────────────────────────────────────────

    public static BookingSettings DefaultBookingSettings => new()
    {
        CheckoutHoldMinutes = 10,
        TaxRate = 0.15m,
        CancellationFreeHours = 24,
        CancellationFeePercent = 0.30m,
        MaxAdvanceBookingDays = 365
    };

    public static PaymentUrlSettings DefaultPaymentUrlSettings => new()
    {
        SuccessUrlTemplate = "https://test.com/booking/{0}/success",
        CancelUrlTemplate = "https://test.com/booking/{0}/cancel"
    };

    public static IOptions<BookingSettings> BookingOptions(BookingSettings? s = null)
        => Options.Create(s ?? DefaultBookingSettings);

    public static IOptions<PaymentUrlSettings> PaymentUrlOptions(PaymentUrlSettings? s = null)
        => Options.Create(s ?? DefaultPaymentUrlSettings);

    // ─── City ────────────────────────────────────────────────────────────

    public static City CreateCity(
        Guid? id = null, string name = "Test City",
        string country = "Test Country", string? postOffice = null)
        => new(id ?? Guid.NewGuid(), name, country, postOffice);

    // ─── Hotel ───────────────────────────────────────────────────────────

    public static Hotel CreateHotel(
        Guid? id = null, Guid? cityId = null,
        string name = "Test Hotel", string owner = "Test Owner",
        string address = "123 Test St", short starRating = 4)
        => new(id ?? Guid.NewGuid(), cityId ?? Guid.NewGuid(),
               name, owner, address, starRating);

    // ─── RoomType ────────────────────────────────────────────────────────

    public static RoomType CreateRoomType(
        Guid? id = null, string name = "Deluxe", string? description = null)
        => new(id ?? Guid.NewGuid(), name, description);

    // ─── Service ─────────────────────────────────────────────────────────

    public static Service CreateService(
        Guid? id = null, string name = "WiFi", string? description = null)
        => new(id ?? Guid.NewGuid(), name, description);

    // ─── HotelRoomType (with navigation wiring) ─────────────────────────

    public static HotelRoomType CreateHotelRoomType(
        Guid? id = null, Guid? hotelId = null, Guid? roomTypeId = null,
        decimal pricePerNight = 150m, short adultCapacity = 2,
        short childCapacity = 0, string? description = null)
    {
        var hotel = CreateHotel(hotelId);
        var roomType = CreateRoomType(roomTypeId);
        var hrt = new HotelRoomType(
            id ?? Guid.NewGuid(), hotel.Id, roomType.Id,
            pricePerNight, adultCapacity, childCapacity, description);

        SetNav(hrt, nameof(HotelRoomType.Hotel), hotel);
        SetNav(hrt, nameof(HotelRoomType.RoomType), roomType);
        return hrt;
    }

    /// <summary>
    /// Creates HotelRoomType wired to specific Hotel and RoomType instances.
    /// </summary>
    public static HotelRoomType CreateHotelRoomTypeFor(
        Hotel hotel, RoomType roomType,
        Guid? id = null, decimal pricePerNight = 150m,
        short adultCapacity = 2, short childCapacity = 0)
    {
        var hrt = new HotelRoomType(
            id ?? Guid.NewGuid(), hotel.Id, roomType.Id,
            pricePerNight, adultCapacity, childCapacity);
        SetNav(hrt, nameof(HotelRoomType.Hotel), hotel);
        SetNav(hrt, nameof(HotelRoomType.RoomType), roomType);
        return hrt;
    }

    // ─── Room ─────────────────────────────────────────────────────
    // FIX: Room constructor has NO status parameter (it defaults to Available)

    public static Room CreateRoom(
        Guid? id = null, Guid? hotelRoomTypeId = null,
        Guid? hotelId = null, string roomNumber = "101")
        => new(id ?? Guid.NewGuid(),
               hotelRoomTypeId ?? Guid.NewGuid(),
               hotelId ?? Guid.NewGuid(),
               roomNumber,
               floor: 1);

    // ─── Booking ─────────────────────────────────────────────────────────

    public static Booking CreateBooking(
        Guid? id = null, Guid? userId = null, Guid? hotelId = null,
        string bookingNumber = "BK-TEST-001",
        decimal totalAmount = 600m,
        DateOnly? checkIn = null, DateOnly? checkOut = null,
        string userEmail = "guest@test.com",
        BookingStatus? status = null)
    {
        var booking = new Booking(
            id: id ?? Guid.NewGuid(),
            bookingNumber: bookingNumber,
            userId: userId ?? Guid.NewGuid(),
            hotelId: hotelId ?? Guid.NewGuid(),
            hotelName: "Test Hotel",
            hotelAddress: "1 Test Ave",
            userEmail: userEmail,
            checkIn: checkIn ?? new DateOnly(2026, 7, 1),
            checkOut: checkOut ?? new DateOnly(2026, 7, 5),
            totalAmount: totalAmount);

        if (status is not null)
        {
            switch (status)
            {
                case BookingStatus.Confirmed: booking.Confirm(); break;
                case BookingStatus.Failed: booking.MarkAsFailed(); break;
                case BookingStatus.Cancelled: booking.Confirm(); booking.Cancel(); break;
                case BookingStatus.CheckedIn: booking.Confirm(); booking.CheckInGuest(); break;
                case BookingStatus.Completed:
                    booking.Confirm(); booking.CheckInGuest(); booking.Complete(); break;
            }
        }
        return booking;
    }

    // ─── Payment ─────────────────────────────────────────────────────────

    public static Payment CreatePayment(
        Guid? id = null, Guid? bookingId = null,
        decimal amount = 600m, PaymentStatus? status = null)
    {
        var payment = new Payment(
            id ?? Guid.NewGuid(), bookingId ?? Guid.NewGuid(),
            amount, PaymentMethod.Stripe);

        if (status == PaymentStatus.Succeeded)
            payment.MarkAsSucceeded("txn_test_" + Guid.NewGuid().ToString("N")[..8]);
        else if (status == PaymentStatus.Failed)
            payment.MarkAsFailed("{\"reason\":\"hard_decline\"}");
        else if (status == PaymentStatus.InitiationFailed)
            payment.MarkInitiationFailed("{\"error\":\"session\"}");

        return payment;
    }

    // ─── Cancellation ────────────────────────────────────────────────────

    public static Cancellation CreateCancellation(
        Guid? id = null, Guid? bookingId = null,
        decimal refundAmount = 420m, decimal refundPercentage = 0.70m,
        string? reason = "Guest changed plans")
        => new(id ?? Guid.NewGuid(), bookingId ?? Guid.NewGuid(),
               reason, refundAmount, refundPercentage);

    // ─── CheckoutHold ────────────────────────────────────────────────────

    public static CheckoutHold CreateHold(
        Guid? id = null, Guid? userId = null,
        Guid? hotelId = null, Guid? hotelRoomTypeId = null,
        DateOnly? checkIn = null, DateOnly? checkOut = null,
        int quantity = 1, int expiryMinutes = 10)
        => new(id ?? Guid.NewGuid(), userId ?? Guid.NewGuid(),
               hotelId ?? Guid.NewGuid(),
               hotelRoomTypeId ?? Guid.NewGuid(),
               checkIn ?? new DateOnly(2026, 7, 1),
               checkOut ?? new DateOnly(2026, 7, 5),
               quantity,
               DateTimeOffset.UtcNow.AddMinutes(expiryMinutes));

    /// <summary>
    /// Creates a CheckoutHold with navigation properties set (for CreateBooking tests).
    /// </summary>
    public static CheckoutHold CreateHoldWithNav(
        Hotel? hotel = null, RoomType? roomType = null,
        Guid? id = null, Guid? userId = null,
        decimal pricePerNight = 100m,
        DateOnly? checkIn = null, DateOnly? checkOut = null,
        int quantity = 1, int expiryMinutes = 10)
    {
        hotel ??= CreateHotel();
        roomType ??= CreateRoomType();
        var hrt = CreateHotelRoomTypeFor(hotel, roomType, pricePerNight: pricePerNight);

        var hold = new CheckoutHold(
            id ?? Guid.NewGuid(), userId ?? Guid.NewGuid(),
            hotel.Id, hrt.Id,
            checkIn ?? new DateOnly(2026, 7, 1),
            checkOut ?? new DateOnly(2026, 7, 3), // 2 nights
            quantity,
            DateTimeOffset.UtcNow.AddMinutes(expiryMinutes));

        SetNav(hold, nameof(CheckoutHold.HotelRoomType), hrt);
        return hold;
    }

    // ─── CartItem ────────────────────────────────────────────────────────

    public static CartItem CreateCartItem(
    Guid? id = null,
    Guid? userId = null,
    Guid? hotelId = null,
    Guid? hotelRoomTypeId = null,
    DateOnly? checkIn = null,
    DateOnly? checkOut = null,
    int quantity = 1,
    int adults = 2,
    int children = 0)
    => new(
        id ?? Guid.NewGuid(),
        userId ?? Guid.NewGuid(),
        hotelId ?? Guid.NewGuid(),
        hotelRoomTypeId ?? Guid.NewGuid(),
        checkIn ?? new DateOnly(2026, 7, 1),
        checkOut ?? new DateOnly(2026, 7, 5),
        quantity,
        adults,
        children);
    // ─── Booking + Payment pair (for webhook/cancel tests) ───────────────

    public static (Booking booking, Payment payment) CreateBookingWithPayment(
        BookingStatus bookingStatus = BookingStatus.Pending,
        PaymentStatus paymentStatus = PaymentStatus.Pending,
        string? providerSessionId = "sess_test",
        string? transactionRef = null,
        string? providerResponseJson = null,
        decimal amount = 230m, string userEmail = "test@test.com")
    {
        var bookingId = Guid.NewGuid();
        var booking = CreateBooking(
            id: bookingId, totalAmount: amount,
            userEmail: userEmail, status: bookingStatus);

        var payment = new Payment(Guid.NewGuid(), bookingId, amount, PaymentMethod.Stripe);

        if (providerSessionId != null)
            payment.SetProviderSession(providerSessionId);

        switch (paymentStatus)
        {
            case PaymentStatus.Succeeded:
                payment.MarkAsSucceeded(transactionRef ?? "txn_test", providerResponseJson);
                break;
            case PaymentStatus.Failed:
                payment.MarkAsFailed(providerResponseJson);
                break;
            case PaymentStatus.InitiationFailed:
                payment.MarkInitiationFailed(providerResponseJson);
                break;
        }

        SetNav(payment, nameof(Payment.Booking), booking);
        return (booking, payment);
    }

    // ─── Mock Helpers ────────────────────────────────────────────────────

    public static Mock<IDbContextTransaction> MockTransaction()
    {
        var m = new Mock<IDbContextTransaction>();
        m.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        m.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        m.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        return m;
    }

    public static void SetupTransaction(Mock<IAppDbContext> db)
    {
        var tx = MockTransaction();
        db.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(tx.Object);
        db.Setup(x => x.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(tx.Object);
    }

    // ─── Reflection: set navigation properties ───────────────────────────

    public static void SetNav<TEntity, TValue>(TEntity entity, string prop, TValue value)
        where TEntity : class
    {
        var p = typeof(TEntity).GetProperty(prop,
            BindingFlags.Public | BindingFlags.Instance);
        p?.SetValue(entity, value);
    }

    public static void SetPrivateProp<TEntity>(TEntity entity, string prop, object? value)
        where TEntity : class
    {
        var type = typeof(TEntity);
        while (type != null)
        {
            var field = type.GetField($"<{prop}>k__BackingField",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null) { field.SetValue(entity, value); return; }

            var pi = type.GetProperty(prop,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (pi != null) { pi.SetValue(entity, value); return; }

            type = type.BaseType;
        }
    }
}
