/// <summary>
/// Tests for the Payment entity — all state transitions, SetProviderSession,
/// CanRecoverFromLocalTimeoutFailure, and constructor validation.
/// </summary>
using FluentAssertions;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Bookings.Enums;
using Xunit;

namespace HotelBooking.Domain.Tests.Bookings;

public class PaymentTests
{
    #region Helpers

    private static Payment CreatePendingPayment(decimal amount = 500m)
        => new(
            id: Guid.NewGuid(),
            bookingId: Guid.NewGuid(),
            amount: amount,
            method: PaymentMethod.Stripe);

    private static Payment CreatePaymentWithStatus(PaymentStatus target)
    {
        var p = CreatePendingPayment();
        switch (target)
        {
            case PaymentStatus.Succeeded:
                p.MarkAsSucceeded("txn_001");
                break;
            case PaymentStatus.Failed:
                p.MarkAsFailed("{\"reason\":\"hard_decline\"}");
                break;
            case PaymentStatus.InitiationFailed:
                p.MarkInitiationFailed("{\"error\":\"session_error\"}");
                break;
            case PaymentStatus.Refunded:
                p.MarkAsSucceeded("txn_001");
                p.MarkAsRefunded();
                break;
            case PaymentStatus.PartiallyRefunded:
                p.MarkAsSucceeded("txn_001");
                p.MarkAsPartiallyRefunded();
                break;
        }
        return p;
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_ValidParams_SetsFieldsCorrectly()
    {
        var id = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var payment = new Payment(id, bookingId, 750m, PaymentMethod.Stripe, "txn_initial");

        payment.Id.Should().Be(id);
        payment.BookingId.Should().Be(bookingId);
        payment.Amount.Should().Be(750m);
        payment.Method.Should().Be(PaymentMethod.Stripe);
        payment.TransactionRef.Should().Be("txn_initial");
    }

    [Fact]
    public void Constructor_DefaultsStatusToPending()
    {
        var payment = CreatePendingPayment();

        payment.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public void Constructor_NegativeAmount_ThrowsArgumentOutOfRange()
    {
        var act = () => new Payment(Guid.NewGuid(), Guid.NewGuid(), -1m, PaymentMethod.Stripe);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region MarkAsSucceeded

    [Fact]
    public void MarkAsSucceeded_WhenPending_SetsSucceeded()
    {
        var payment = CreatePendingPayment();

        payment.MarkAsSucceeded("txn_abc");

        payment.Status.Should().Be(PaymentStatus.Succeeded);
        payment.TransactionRef.Should().Be("txn_abc");
    }

    [Fact]
    public void MarkAsSucceeded_SetsPaidAtUtc()
    {
        var payment = CreatePendingPayment();
        var before = DateTimeOffset.UtcNow;

        payment.MarkAsSucceeded("txn_abc");

        payment.PaidAtUtc.Should().NotBeNull();
        payment.PaidAtUtc!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void MarkAsSucceeded_EmptyTransactionRef_ThrowsArgument()
    {
        var payment = CreatePendingPayment();

        var act = () => payment.MarkAsSucceeded("   ");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Succeeded)]
    [InlineData(PaymentStatus.InitiationFailed)]
    public void MarkAsSucceeded_WhenNotPending_Throws(PaymentStatus startStatus)
    {
        var payment = CreatePaymentWithStatus(startStatus);

        var act = () => payment.MarkAsSucceeded("txn_late");

        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region RecoverSucceededFromFailed

    [Fact]
    public void RecoverSucceededFromFailed_WhenFailed_SetsSucceeded()
    {
        var payment = CreatePaymentWithStatus(PaymentStatus.Failed);

        payment.RecoverSucceededFromFailed("txn_recover");

        payment.Status.Should().Be(PaymentStatus.Succeeded);
        payment.TransactionRef.Should().Be("txn_recover");
    }

    [Fact]
    public void RecoverSucceededFromFailed_EmptyRef_ThrowsArgument()
    {
        var payment = CreatePaymentWithStatus(PaymentStatus.Failed);

        var act = () => payment.RecoverSucceededFromFailed("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RecoverSucceededFromFailed_PreservesPaidAtUtcIfAlreadySet()
    {
        var payment = CreatePaymentWithStatus(PaymentStatus.Failed);
        // PaidAtUtc should be null in failed state; after recovery it should be set
        payment.RecoverSucceededFromFailed("txn_rec");

        payment.PaidAtUtc.Should().NotBeNull();
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Succeeded)]
    [InlineData(PaymentStatus.InitiationFailed)]
    public void RecoverSucceededFromFailed_WhenNotFailed_Throws(PaymentStatus startStatus)
    {
        var payment = CreatePaymentWithStatus(startStatus);

        var act = () => payment.RecoverSucceededFromFailed("txn_late");

        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region MarkAsFailed

    [Fact]
    public void MarkAsFailed_WhenPending_SetsFailed()
    {
        var payment = CreatePendingPayment();

        payment.MarkAsFailed("{\"reason\":\"network\"}");

        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.ProviderResponseJson.Should().Be("{\"reason\":\"network\"}");
    }

    [Fact]
    public void MarkAsFailed_WhenInitiationFailed_SetsFailed()
    {
        var payment = CreatePaymentWithStatus(PaymentStatus.InitiationFailed);

        payment.MarkAsFailed("{\"reason\":\"timeout\"}");

        payment.Status.Should().Be(PaymentStatus.Failed);
    }

    [Theory]
    [InlineData(PaymentStatus.Succeeded)]
    [InlineData(PaymentStatus.Failed)]
    public void MarkAsFailed_WhenSucceededOrAlreadyFailed_Throws(PaymentStatus startStatus)
    {
        var payment = CreatePaymentWithStatus(startStatus);

        var act = () => payment.MarkAsFailed("json");

        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region MarkAsRefunded / MarkAsPartiallyRefunded

    [Fact]
    public void MarkAsRefunded_WhenSucceeded_SetsRefunded()
    {
        var payment = CreatePaymentWithStatus(PaymentStatus.Succeeded);

        payment.MarkAsRefunded();

        payment.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.InitiationFailed)]
    public void MarkAsRefunded_WhenNotSucceeded_Throws(PaymentStatus startStatus)
    {
        var payment = CreatePaymentWithStatus(startStatus);

        var act = () => payment.MarkAsRefunded();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkAsPartiallyRefunded_WhenSucceeded_Sets()
    {
        var payment = CreatePaymentWithStatus(PaymentStatus.Succeeded);

        payment.MarkAsPartiallyRefunded();

        payment.Status.Should().Be(PaymentStatus.PartiallyRefunded);
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Failed)]
    public void MarkAsPartiallyRefunded_WhenNotSucceeded_Throws(PaymentStatus startStatus)
    {
        var payment = CreatePaymentWithStatus(startStatus);

        var act = () => payment.MarkAsPartiallyRefunded();

        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region MarkInitiationFailed

    [Fact]
    public void MarkInitiationFailed_WhenPending_SetsInitiationFailed()
    {
        var payment = CreatePendingPayment();

        payment.MarkInitiationFailed("{\"error\":\"session\"}");

        payment.Status.Should().Be(PaymentStatus.InitiationFailed);
    }

    [Theory]
    [InlineData(PaymentStatus.Succeeded)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.InitiationFailed)]
    public void MarkInitiationFailed_WhenNotPending_Throws(PaymentStatus startStatus)
    {
        var payment = CreatePaymentWithStatus(startStatus);

        var act = () => payment.MarkInitiationFailed();

        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region SetProviderSession

    [Fact]
    public void SetProviderSession_FirstTime_SetsValue()
    {
        var payment = CreatePendingPayment();

        payment.SetProviderSession("cs_test_abc123");

        payment.ProviderSessionId.Should().Be("cs_test_abc123");
    }

    [Fact]
    public void SetProviderSession_SameValue_Idempotent()
    {
        var payment = CreatePendingPayment();
        payment.SetProviderSession("cs_test_abc123");

        var act = () => payment.SetProviderSession("cs_test_abc123");

        act.Should().NotThrow();
    }

    [Fact]
    public void SetProviderSession_DifferentValue_Throws()
    {
        var payment = CreatePendingPayment();
        payment.SetProviderSession("cs_original");

        var act = () => payment.SetProviderSession("cs_different");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SetProviderSession_EmptyString_ThrowsArgument()
    {
        var payment = CreatePendingPayment();

        var act = () => payment.SetProviderSession("   ");

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region CanRecoverFromLocalTimeoutFailure

    [Fact]
    public void CanRecover_WhenFailedWithPaymentTimeout_ReturnsTrue()
    {
        var payment = CreatePendingPayment();
        payment.MarkAsFailed("{\"reason\":\"payment_timeout\"}");

        payment.CanRecoverFromLocalTimeoutFailure().Should().BeTrue();
    }

    [Fact]
    public void CanRecover_WhenFailedWithInitiationTimeout_ReturnsTrue()
    {
        var payment = CreatePaymentWithStatus(PaymentStatus.InitiationFailed);
        payment.MarkAsFailed("{\"reason\":\"payment_initiation_timeout\"}");

        payment.CanRecoverFromLocalTimeoutFailure().Should().BeTrue();
    }

    [Fact]
    public void CanRecover_WhenFailedWithOtherJson_ReturnsFalse()
    {
        var payment = CreatePendingPayment();
        payment.MarkAsFailed("{\"reason\":\"hard_decline\"}");

        payment.CanRecoverFromLocalTimeoutFailure().Should().BeFalse();
    }

    [Fact]
    public void CanRecover_WhenFailedWithNullJson_ReturnsFalse()
    {
        var payment = CreatePendingPayment();
        payment.MarkAsFailed(null);

        payment.CanRecoverFromLocalTimeoutFailure().Should().BeFalse();
    }

    [Fact]
    public void CanRecover_WhenNotFailed_ReturnsFalse()
    {
        var payment = CreatePendingPayment();

        payment.CanRecoverFromLocalTimeoutFailure().Should().BeFalse();
    }

    #endregion
}
