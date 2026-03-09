using System.Data;
using FluentAssertions;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models.Payment;
using HotelBooking.Application.Features.Checkout.Commands.CancelBooking;
using HotelBooking.Application.Features.Checkout.Commands.CreateBooking;
using HotelBooking.Application.Features.Checkout.Commands.ExpirePendingPayments;
using HotelBooking.Application.Features.Checkout.Commands.HandlePaymentWebhook;
using HotelBooking.Application.Settings;
using HotelBooking.Application.Tests._Shared;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Cart;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace HotelBooking.Application.Tests.Checkout;

public sealed class CreateBookingCommandHandlerCoverageTests
{
    private readonly Mock<IAppDbContext> _db = new();
    private readonly Mock<IPaymentGateway> _gw = new();
    private readonly Mock<ILogger<CreateBookingCommandHandler>> _log = new();

    private readonly List<Payment> _payments = [];
    private readonly List<Booking> _bookings = [];
    private readonly List<BookingRoom> _bookingRooms = [];

    public CreateBookingCommandHandlerCoverageTests()
    {
        _gw.SetupGet(x => x.ProviderName).Returns("Stripe");
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var tx = TestHelpers.MockTransaction();
        _db.Setup(x => x.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx.Object);
        _db.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx.Object);

        SetupTrackableSets();
    }

    private void SetupTrackableSets()
    {
        var bookingsSet = _bookings.AsQueryable().BuildMockDbSet();
        bookingsSet.Setup(x => x.Add(It.IsAny<Booking>()))
            .Callback<Booking>(b => _bookings.Add(b));
        _db.Setup(x => x.Bookings).Returns(bookingsSet.Object);

        var bookingRoomsSet = _bookingRooms.AsQueryable().BuildMockDbSet();
        bookingRoomsSet.Setup(x => x.AddRange(It.IsAny<IEnumerable<BookingRoom>>()))
            .Callback<IEnumerable<BookingRoom>>(x => _bookingRooms.AddRange(x));
        _db.Setup(x => x.BookingRooms).Returns(bookingRoomsSet.Object);

        var paymentsSet = _payments.AsQueryable().BuildMockDbSet();
        paymentsSet.Setup(x => x.Add(It.IsAny<Payment>()))
            .Callback<Payment>(p => _payments.Add(p));
        _db.Setup(x => x.Payments).Returns(paymentsSet.Object);
    }

    private CreateBookingCommandHandler Sut() => new(
        _db.Object,
        _gw.Object,
        TestHelpers.BookingOptions(),
        TestHelpers.PaymentUrlOptions(),
        _log.Object);

    private static (CheckoutHold Hold, Room Room) CreateValidHoldAndRoom()
    {
        var hold = TestHelpers.CreateHoldWithNav(pricePerNight: 100m);
        var room = TestHelpers.CreateRoom(
            hotelRoomTypeId: hold.HotelRoomTypeId,
            hotelId: hold.HotelId);
        return (hold, room);
    }

    private void SetupValidPhaseA(CheckoutHold hold, Room room)
    {
        _db.Setup(x => x.CheckoutHolds).Returns(
            new List<CheckoutHold> { hold }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.Rooms).Returns(
            new List<Room> { room }.AsQueryable().BuildMockDbSet().Object);
    }

    [Fact]
    public async Task Handle_PhaseAOperationCanceled_WithRollbackFailure_ThrowsOperationCanceled()
    {
        var (hold, room) = CreateValidHoldAndRoom();
        SetupValidPhaseA(hold, room);

        var tx = TestHelpers.MockTransaction();
        tx.Setup(x => x.RollbackAsync(CancellationToken.None))
            .ThrowsAsync(new Exception("rollback failed"));

        _db.Setup(x => x.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx.Object);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var act = async () => await Sut().Handle(
            new CreateBookingCommand(hold.UserId, "u@test.com", [hold.Id], null),
            default);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Handle_PhaseAUnexpectedError_RollsBackAndRethrows()
    {
        var (hold, room) = CreateValidHoldAndRoom();
        SetupValidPhaseA(hold, room);

        var tx = TestHelpers.MockTransaction();
        _db.Setup(x => x.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx.Object);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("phaseA failed"));

        var act = async () => await Sut().Handle(
            new CreateBookingCommand(hold.UserId, "u@test.com", [hold.Id], null),
            default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_PhaseAUnexpectedError_WithRollbackFailure_StillRethrowsOriginal()
    {
        var (hold, room) = CreateValidHoldAndRoom();
        SetupValidPhaseA(hold, room);

        var tx = TestHelpers.MockTransaction();
        tx.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("rollback failed"));

        _db.Setup(x => x.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx.Object);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("phaseA failed"));

        var act = async () => await Sut().Handle(
            new CreateBookingCommand(hold.UserId, "u@test.com", [hold.Id], null),
            default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_PersistPaymentSessionCanceled_ThrowsOperationCanceled()
    {
        var (hold, room) = CreateValidHoldAndRoom();
        SetupValidPhaseA(hold, room);

        _gw.Setup(x => x.CreatePaymentSessionAsync(
                It.IsAny<PaymentSessionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentSessionResponse("sess-persist-cancel", "https://pay"));

        var tx = TestHelpers.MockTransaction();
        _db.Setup(x => x.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx.Object);
        _db.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var act = async () => await Sut().Handle(
            new CreateBookingCommand(hold.UserId, "u@test.com", [hold.Id], null),
            default);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Handle_GatewayFailure_WhenMarkInitiationCanceled_ThrowsOperationCanceled()
    {
        var (hold, room) = CreateValidHoldAndRoom();
        SetupValidPhaseA(hold, room);

        _gw.Setup(x => x.CreatePaymentSessionAsync(
                It.IsAny<PaymentSessionRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("gateway down"));

        var txPhaseA = TestHelpers.MockTransaction();
        _db.Setup(x => x.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(txPhaseA.Object);
        _db.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var act = async () => await Sut().Handle(
            new CreateBookingCommand(hold.UserId, "u@test.com", [hold.Id], null),
            default);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

public sealed class CancelBookingCommandHandlerCoverageTests
{
    private readonly Mock<IAppDbContext> _db = new();
    private readonly Mock<IPaymentGateway> _gw = new();
    private readonly Mock<ILogger<CancelBookingCommandHandler>> _log = new();
    private Cancellation? _addedCancellation;

    public CancelBookingCommandHandlerCoverageTests()
    {
        TestHelpers.SetupTransaction(_db);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var cancellations = new List<Cancellation>().AsQueryable().BuildMockDbSet();
        cancellations.Setup(x => x.Add(It.IsAny<Cancellation>()))
            .Callback<Cancellation>(c => _addedCancellation = c);
        _db.Setup(x => x.Cancellations).Returns(cancellations.Object);
    }

    private CancelBookingCommandHandler Sut(BookingSettings? settings = null) =>
        new(_db.Object, _gw.Object, TestHelpers.BookingOptions(settings), _log.Object);

    private void SetupBookingWithReload(Booking booking, Payment? payment = null, bool failReload = false)
    {
        var payments = payment is null ? new List<Payment>() : [payment];
        TestHelpers.SetNav(booking, "Payments", payments);
        TestHelpers.SetNav(booking, "Cancellation", (Cancellation?)null);

        var first = new List<Booking> { booking }.AsQueryable().BuildMockDbSet();
        var second = failReload
            ? new List<Booking>().AsQueryable().BuildMockDbSet()
            : new List<Booking> { booking }.AsQueryable().BuildMockDbSet();

        _db.SetupSequence(x => x.Bookings)
            .Returns(first.Object)
            .Returns(second.Object);

        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                if (_addedCancellation is not null)
                    TestHelpers.SetNav(booking, "Cancellation", _addedCancellation);
            })
            .ReturnsAsync(1);
    }

    private static Booking CreateConfirmedBooking(int confirmedMinutesAgo = 10, decimal total = 200m)
    {
        var booking = TestHelpers.CreateBooking(
            totalAmount: total,
            checkIn: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            checkOut: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(16)));

        booking.Confirm();
        TestHelpers.SetPrivateProp(booking, "LastModifiedUtc", DateTimeOffset.UtcNow.AddMinutes(-confirmedMinutesAgo));
        return booking;
    }

    private static Payment CreateSucceededPayment(Booking booking, decimal amount)
    {
        var payment = TestHelpers.CreatePayment(bookingId: booking.Id, amount: amount);
        payment.SetProviderSession("sess");
        payment.MarkAsSucceeded("txn_ok");
        TestHelpers.SetNav(payment, "Booking", booking);
        return payment;
    }

    [Fact]
    public async Task Handle_AlreadyCancelledWithoutCancellation_ReturnsCancellationStateInvalid()
    {
        var booking = TestHelpers.CreateBooking(status: BookingStatus.Cancelled);
        TestHelpers.SetNav(booking, "Cancellation", (Cancellation?)null);
        TestHelpers.SetNav(booking, "Payments", new List<Payment>());

        _db.Setup(x => x.Bookings).Returns(
            new List<Booking> { booking }.AsQueryable().BuildMockDbSet().Object);

        var result = await Sut().Handle(
            new CancelBookingCommand(booking.Id, booking.UserId, false, null),
            default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Booking.CancellationStateInvalid");
    }

    [Fact]
    public async Task Handle_CancellationCreatedButReloadFails_ReturnsCancellationReloadFailed()
    {
        var booking = CreateConfirmedBooking(total: 180m);
        SetupBookingWithReload(booking, payment: null, failReload: true);

        var result = await Sut().Handle(
            new CancelBookingCommand(booking.Id, booking.UserId, false, null),
            default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Booking.CancellationReloadFailed");
    }

    [Fact]
    public async Task Handle_ZeroAmountRefund_MarksProcessedWithoutGatewayCall()
    {
        var booking = CreateConfirmedBooking(confirmedMinutesAgo: 60 * 24 * 3, total: 200m);
        SetupBookingWithReload(booking);

        var settings = new BookingSettings
        {
            CheckoutHoldMinutes = 10,
            TaxRate = 0.15m,
            CancellationFreeHours = 0,
            CancellationFeePercent = 1.0m,
            MaxAdvanceBookingDays = 365
        };

        var result = await Sut(settings).Handle(
            new CancelBookingCommand(booking.Id, booking.UserId, false, "  reason  "),
            default);

        result.IsError.Should().BeFalse();
        result.Value.RefundAmount.Should().Be(0m);
        result.Value.RefundStatus.Should().Be(RefundStatus.Processed.ToString());
        result.Value.Reason.Should().Be("reason");
        _gw.Verify(x => x.RefundAsync(
            It.IsAny<string>(),
            It.IsAny<decimal>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RefundProviderNonSuccess_KeepsRefundPending()
    {
        var booking = CreateConfirmedBooking(total: 200m);
        var payment = CreateSucceededPayment(booking, 200m);
        SetupBookingWithReload(booking, payment);

        _gw.Setup(x => x.RefundAsync(
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefundResponse(false, null, "provider-failed"));

        var result = await Sut().Handle(
            new CancelBookingCommand(booking.Id, booking.UserId, false, null),
            default);

        result.IsError.Should().BeFalse();
        result.Value.RefundStatus.Should().Be(RefundStatus.Pending.ToString());
    }

    [Fact]
    public async Task Handle_RefundOperationCanceled_ThrowsOperationCanceled()
    {
        var booking = CreateConfirmedBooking(total: 200m);
        var payment = CreateSucceededPayment(booking, 200m);
        SetupBookingWithReload(booking, payment);

        _gw.Setup(x => x.RefundAsync(
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var act = async () => await Sut().Handle(
            new CancelBookingCommand(booking.Id, booking.UserId, false, null),
            default);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

public sealed class ExpirePendingPaymentsCommandHandlerCoverageTests
{
    private readonly Mock<IAppDbContext> _db = new();
    private readonly Mock<ILogger<ExpirePendingPaymentsCommandHandler>> _log = new();

    public ExpirePendingPaymentsCommandHandlerCoverageTests()
    {
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _db.Setup(x => x.ClearChangeTracker());
        _db.Setup(x => x.ReloadEntityAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _db.Setup(x => x.ReloadEntityAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private ExpirePendingPaymentsCommandHandler Sut() =>
        new(_db.Object, TestHelpers.BookingOptions(), _log.Object);

    private static Payment CreateOldPendingPayment(Booking booking, Guid? id = null)
    {
        var payment = TestHelpers.CreatePayment(id: id, bookingId: booking.Id, amount: 100m);
        TestHelpers.SetNav(payment, "Booking", booking);
        TestHelpers.SetPrivateProp(payment, "CreatedAtUtc", DateTimeOffset.UtcNow.AddMinutes(-45));
        return payment;
    }

    [Fact]
    public async Task Handle_FallbackExpireOne_WhenPaymentMissing_ReturnsUpdated()
    {
        var booking = TestHelpers.CreateBooking();
        var id = Guid.NewGuid();
        var candidate = CreateOldPendingPayment(booking, id);

        var tx = TestHelpers.MockTransaction();
        _db.SetupSequence(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("batch race"))
            .ReturnsAsync(tx.Object);

        _db.SetupSequence(x => x.Payments)
            .Returns(new List<Payment> { candidate }.AsQueryable().BuildMockDbSet().Object)
            .Returns(new List<Payment>().AsQueryable().BuildMockDbSet().Object);

        var result = await Sut().Handle(new ExpirePendingPaymentsCommand(100), default);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_FallbackExpireOne_WhenPaymentNoLongerExpired_ReturnsUpdated()
    {
        var booking = TestHelpers.CreateBooking();
        var id = Guid.NewGuid();
        var candidate = CreateOldPendingPayment(booking, id);
        var nowPayment = TestHelpers.CreatePayment(id: id, bookingId: booking.Id, amount: 100m);
        TestHelpers.SetNav(nowPayment, "Booking", booking);
        TestHelpers.SetPrivateProp(nowPayment, "CreatedAtUtc", DateTimeOffset.UtcNow.AddMinutes(5));

        var tx = TestHelpers.MockTransaction();
        _db.SetupSequence(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("batch race"))
            .ReturnsAsync(tx.Object);

        _db.SetupSequence(x => x.Payments)
            .Returns(new List<Payment> { candidate }.AsQueryable().BuildMockDbSet().Object)
            .Returns(new List<Payment> { nowPayment }.AsQueryable().BuildMockDbSet().Object);

        var result = await Sut().Handle(new ExpirePendingPaymentsCommand(100), default);

        result.IsError.Should().BeFalse();
        nowPayment.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task Handle_FallbackExpireOne_InitiationFailed_ExpiresSuccessfully()
    {
        var booking = TestHelpers.CreateBooking();
        var payment = CreateOldPendingPayment(booking);
        payment.MarkInitiationFailed("{\"error\":\"init\"}");

        var tx = TestHelpers.MockTransaction();
        _db.SetupSequence(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("batch race"))
            .ReturnsAsync(tx.Object);

        _db.SetupSequence(x => x.Payments)
            .Returns(new List<Payment> { payment }.AsQueryable().BuildMockDbSet().Object)
            .Returns(new List<Payment> { payment }.AsQueryable().BuildMockDbSet().Object);

        var result = await Sut().Handle(new ExpirePendingPaymentsCommand(100), default);

        result.IsError.Should().BeFalse();
        payment.Status.Should().Be(PaymentStatus.Failed);
        booking.Status.Should().Be(BookingStatus.Failed);
    }

    [Fact]
    public async Task Handle_FallbackExpireOne_SaveConcurrencyConflict_IsSwallowed()
    {
        var booking = TestHelpers.CreateBooking();
        var payment = CreateOldPendingPayment(booking);

        var tx = TestHelpers.MockTransaction();
        _db.SetupSequence(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("batch race"))
            .ReturnsAsync(tx.Object);

        _db.SetupSequence(x => x.Payments)
            .Returns(new List<Payment> { payment }.AsQueryable().BuildMockDbSet().Object)
            .Returns(new List<Payment> { payment }.AsQueryable().BuildMockDbSet().Object);

        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("item race"));

        var result = await Sut().Handle(new ExpirePendingPaymentsCommand(100), default);

        result.IsError.Should().BeFalse();
        _db.Verify(x => x.ClearChangeTracker(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_FallbackExpireOne_OperationCanceled_Rethrows()
    {
        var booking = TestHelpers.CreateBooking();
        var payment = CreateOldPendingPayment(booking);

        var tx = TestHelpers.MockTransaction();
        _db.SetupSequence(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("batch race"))
            .ReturnsAsync(tx.Object);

        _db.SetupSequence(x => x.Payments)
            .Returns(new List<Payment> { payment }.AsQueryable().BuildMockDbSet().Object)
            .Returns(new List<Payment> { payment }.AsQueryable().BuildMockDbSet().Object);

        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await Sut().Handle(new ExpirePendingPaymentsCommand(100), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Handle_FallbackExpireOne_UnexpectedFailure_IsSwallowed()
    {
        var booking = TestHelpers.CreateBooking();
        var payment = CreateOldPendingPayment(booking);

        var tx = TestHelpers.MockTransaction();
        _db.SetupSequence(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("batch race"))
            .ReturnsAsync(tx.Object);

        _db.SetupSequence(x => x.Payments)
            .Returns(new List<Payment> { payment }.AsQueryable().BuildMockDbSet().Object)
            .Returns(new List<Payment> { payment }.AsQueryable().BuildMockDbSet().Object);

        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("save failed"));

        var result = await Sut().Handle(new ExpirePendingPaymentsCommand(100), default);

        result.IsError.Should().BeFalse();
    }
}

public sealed class HandlePaymentWebhookCommandHandlerCoverageTests
{
    private readonly Mock<IAppDbContext> _db = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<ILogger<HandlePaymentWebhookCommandHandler>> _log = new();

    private HandlePaymentWebhookCommandHandler Sut() =>
        new(_db.Object, _email.Object, _log.Object);

    [Fact]
    public async Task Handle_SaveChangesInvalidOperation_ReturnsUpdated()
    {
        var (_, payment) = TestHelpers.CreateBookingWithPayment(providerSessionId: "sess-io");

        _db.Setup(x => x.Payments).Returns(
            new List<Payment> { payment }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.BookingRooms).Returns(
            new List<BookingRoom>().AsQueryable().BuildMockDbSet().Object);

        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("state transition"));

        var evt = new WebhookParseResult(
            true,
            PaymentEventTypes.PaymentSucceeded,
            "sess-io",
            "tx-io",
            "{}");

        var result = await Sut().Handle(new HandlePaymentWebhookCommand(evt), default);

        result.IsError.Should().BeFalse();
    }
}
