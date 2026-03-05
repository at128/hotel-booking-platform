using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Common;

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
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount cannot be negative.");

        BookingId = bookingId;
        Amount = amount;
        Method = method;
        TransactionRef = transactionRef;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        Status = PaymentStatus.Pending;
    }

    public Guid BookingId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }

    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;

    // Provider References
    public string? TransactionRef { get; private set; }       // Stripe PaymentIntent ID, etc.
    public string? ProviderSessionId { get; private set; }    // Stripe Checkout Session ID
    public string? ProviderResponseJson { get; private set; } // Raw webhook / provider response payload

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? PaidAtUtc { get; private set; }

    public Booking Booking { get; private set; } = null!;

    public byte[] RowVersion { get; private set; } = [];

    /// <summary>
    /// Normal success transition (used for timely success webhook / success confirmation).
    /// </summary>
    public void MarkAsSucceeded(string transactionRef, string? responseJson = null)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot succeed payment in {Status} status.");

        if (string.IsNullOrWhiteSpace(transactionRef))
            throw new ArgumentException("Transaction reference cannot be empty.", nameof(transactionRef));

        Status = PaymentStatus.Succeeded;
        TransactionRef = transactionRef;
        ProviderResponseJson = responseJson;
        PaidAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Recovery transition for late provider success after local timeout/failure.
    /// Intended for webhook reconciliation only.
    /// </summary>
    public void RecoverSucceededFromFailed(string transactionRef, string? responseJson = null)
    {
        if (Status != PaymentStatus.Failed)
            throw new InvalidOperationException($"Cannot recover-succeed payment in {Status} status.");

        if (string.IsNullOrWhiteSpace(transactionRef))
            throw new ArgumentException("Transaction reference cannot be empty.", nameof(transactionRef));

        Status = PaymentStatus.Succeeded;
        TransactionRef = transactionRef;
        ProviderResponseJson = responseJson;
        PaidAtUtc ??= DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Used by webhook handler to allow recovery only when previous failure
    /// was caused by local timeout logic (not provider hard decline).
    /// </summary>
    public bool CanRecoverFromLocalTimeoutFailure()
    {
        if (Status != PaymentStatus.Failed)
            return false;

        if (string.IsNullOrWhiteSpace(ProviderResponseJson))
            return false;

        // Compatible with current local timeout job payloads such as:
        // {"reason":"payment_timeout"}
        // {"reason":"payment_initiation_timeout"}
        return ProviderResponseJson.Contains("payment_timeout", StringComparison.OrdinalIgnoreCase)
            || ProviderResponseJson.Contains("payment_initiation_timeout", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Marks payment as failed from Pending or InitiationFailed.
    /// This matches webhook failure path and local timeout expiry path.
    /// </summary>
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
            // Idempotent re-assign with same value
            if (ProviderSessionId == sessionId)
                return;

            throw new InvalidOperationException(
                $"Payment already has provider session '{ProviderSessionId}'.");
        }

        ProviderSessionId = sessionId;
    }

    /// <summary>
    /// Marks payment as initiation-failed (e.g., provider session creation failure).
    /// </summary>
    public void MarkInitiationFailed(string? responseJson = null)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot mark initiation failed in {Status} status.");

        Status = PaymentStatus.InitiationFailed;
        ProviderResponseJson = responseJson;
    }

    /// <summary>
    /// Backward-compatible alias if some handlers/services use MarkAsInitiationFailed().
    /// Keeps your codebase compiling during refactor.
    /// </summary>
    public void MarkAsInitiationFailed(string? responseJson = null)
        => MarkInitiationFailed(responseJson);
}