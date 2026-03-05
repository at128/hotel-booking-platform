using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Domain.Bookings;

public class Payment : Entity
{
    private Payment() { }

    public Payment(
        Guid id,
        Guid bookingId,
        decimal amount,
        PaymentMethod method,
        string? transactionRef = null)
        : base(id)
    {
        BookingId = bookingId;
        Amount = amount;
        Method = method;
        TransactionRef = transactionRef;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid BookingId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;

    // Provider References
    public string? TransactionRef { get; private set; }      // Stripe Payment Intent ID, etc.
    public string? ProviderSessionId { get; private set; }   // Stripe Checkout Session ID
    public string? ProviderResponseJson { get; private set; } // Raw webhook response

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? PaidAtUtc { get; private set; }

    public Booking Booking { get; private set; } = null!;

    public byte[] RowVersion { get; private set; } = [];

    public void MarkAsSucceeded(string transactionRef, string? responseJson = null)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot succeed payment in {Status} status.");
        Status = PaymentStatus.Succeeded;
        TransactionRef = transactionRef;
        ProviderResponseJson = responseJson;
        PaidAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkAsFailed(string? responseJson = null)
    {
        if (Status != PaymentStatus.Pending && Status != PaymentStatus.InitiationFailed)
            throw new InvalidOperationException($"Cannot fail payment in {Status} status.");

        Status = PaymentStatus.Failed;
        ProviderResponseJson = responseJson;
    }

    public void MarkAsRefunded()
    {
        if (Status != PaymentStatus.Succeeded)
            throw new InvalidOperationException($"Cannot refund payment in {Status} status.");
        Status = PaymentStatus.Refunded;
    }

    public void MarkAsPartiallyRefunded()
    {
        if (Status != PaymentStatus.Succeeded)
            throw new InvalidOperationException($"Cannot partially refund payment in {Status} status.");
        Status = PaymentStatus.PartiallyRefunded;
    }

    public void SetProviderSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Provider session id cannot be empty.", nameof(sessionId));

        if (!string.IsNullOrWhiteSpace(ProviderSessionId))
        {
            if (ProviderSessionId == sessionId)
                return; 

            throw new InvalidOperationException(
                $"Payment already has provider session '{ProviderSessionId}'.");
        }

        ProviderSessionId = sessionId;
    }

    public void MarkInitiationFailed(string? responseJson = null)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot mark initiation failed in {Status} status.");

        Status = PaymentStatus.InitiationFailed;
        ProviderResponseJson = responseJson;
    }
}
