namespace HotelBooking.Application.Common.Models.Payment;

public sealed record WebhookParseResult(
    bool IsSignatureValid,

    string EventType,

    string? ProviderSessionId,

    string? TransactionRef,

    string RawPayload
);

public static class PaymentEventTypes
{
    public const string PaymentSucceeded = "payment.succeeded";
    public const string PaymentFailed = "payment.failed";
}