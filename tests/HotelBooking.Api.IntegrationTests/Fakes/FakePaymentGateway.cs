using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models.Payment;
using Xunit;

namespace HotelBooking.Api.IntegrationTests.Fakes;

public class FakePaymentGateway : IPaymentGateway
{
    public string ProviderName => "FakeGateway";

    public List<PaymentSessionRequest> CreatedSessions { get; } = [];
    public List<(string TransactionRef, decimal Amount, string IdempotencyKey)> Refunds { get; } = [];
    public List<string> ExpiredSessions { get; } = [];

    /// <summary>
    /// Configure the next webhook parse result for testing webhook scenarios.
    /// </summary>
    public WebhookParseResult? NextWebhookParseResult { get; set; }

    /// <summary>
    /// If true, RefundAsync returns success. Default: true.
    /// </summary>
    public bool RefundSucceeds { get; set; } = true;

    private int _sessionCounter;

    public Task<PaymentSessionResponse> CreatePaymentSessionAsync(
        PaymentSessionRequest request, CancellationToken ct = default)
    {
        CreatedSessions.Add(request);
        var sessionId = $"fake_session_{Interlocked.Increment(ref _sessionCounter)}";
        var paymentUrl = $"https://fake-stripe.com/checkout/{sessionId}";
        return Task.FromResult(new PaymentSessionResponse(sessionId, paymentUrl));
    }

    public Task<WebhookParseResult> ParseWebhookAsync(
        string rawPayload, string signature, CancellationToken ct = default)
    {
        if (NextWebhookParseResult is not null)
            return Task.FromResult(NextWebhookParseResult);

        // Default: valid signature, payment succeeded
        return Task.FromResult(new WebhookParseResult(
            IsSignatureValid: !string.IsNullOrEmpty(signature),
            EventType: PaymentEventTypes.PaymentSucceeded,
            ProviderSessionId: null,
            TransactionRef: null,
            RawPayload: rawPayload));
    }

    public Task<RefundResponse> RefundAsync(
        string transactionRef, decimal amount, string idempotencyKey,
        CancellationToken ct = default)
    {
        Refunds.Add((transactionRef, amount, idempotencyKey));

        return Task.FromResult(RefundSucceeds
            ? new RefundResponse(true, $"refund_{Guid.NewGuid():N}", null)
            : new RefundResponse(false, null, "Refund failed in fake gateway"));
    }

    public Task ExpirePaymentSessionAsync(string sessionId, CancellationToken ct = default)
    {
        ExpiredSessions.Add(sessionId);
        return Task.CompletedTask;
    }

    public void Reset()
    {
        CreatedSessions.Clear();
        Refunds.Clear();
        ExpiredSessions.Clear();
        NextWebhookParseResult = null;
        RefundSucceeds = true;
        _sessionCounter = 0;
    }
}
