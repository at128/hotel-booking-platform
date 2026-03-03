using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models.Payment;
using HotelBooking.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace HotelBooking.Infrastructure.Payment;

/// <summary>
/// Stripe implementation of IPaymentGateway.
/// All Stripe SDK usage is isolated here — Application layer is completely unaware of Stripe.
/// </summary>
internal sealed class StripePaymentGateway(
    IOptions<StripeSettings> options,
    ILogger<StripePaymentGateway> logger)
    : IPaymentGateway
{
    private readonly StripeSettings _settings = options.Value;

    public string ProviderName => "Stripe";

    public async Task<PaymentSessionResponse> CreatePaymentSessionAsync(
        PaymentSessionRequest request,
        CancellationToken ct = default)
    {
        var sessionOptions = new SessionCreateOptions
        {
            PaymentMethodTypes = ["card"],
            Mode = "payment",
            CustomerEmail = request.CustomerEmail,
            Currency = _settings.Currency,
            ClientReferenceId = request.BookingId.ToString(),
            LineItems =
            [
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = _settings.Currency,
                        UnitAmountDecimal = request.AmountInUsd * 100, // Stripe uses cents
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"{request.HotelName} — {request.BookingNumber}",
                            Description =
                                $"Check-in: {request.CheckIn:yyyy-MM-dd} / Check-out: {request.CheckOut:yyyy-MM-dd}"
                        }
                    },
                    Quantity = 1
                }
            ],
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            Metadata = new Dictionary<string, string>
            {
                ["booking_id"] = request.BookingId.ToString(),
                ["booking_number"] = request.BookingNumber
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(sessionOptions, cancellationToken: ct);

        logger.LogDebug(
            "Stripe session {SessionId} created for booking {BookingNumber}",
            session.Id, request.BookingNumber);

        return new PaymentSessionResponse(
            SessionId: session.Id,
            PaymentUrl: session.Url);
    }

    public Task<WebhookParseResult> ParseWebhookAsync(
        string rawPayload,
        string signature,
        CancellationToken ct = default)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                rawPayload,
                signature,
                _settings.WebhookSecret,
                throwOnApiVersionMismatch: false);

            var (eventType, sessionId, transactionRef) = MapStripeEvent(stripeEvent);

            return Task.FromResult(new WebhookParseResult(
                IsSignatureValid: true,
                EventType: eventType,
                ProviderSessionId: sessionId,
                TransactionRef: transactionRef,
                RawPayload: rawPayload));
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Stripe webhook signature validation failed");
            return Task.FromResult(new WebhookParseResult(
                IsSignatureValid: false,
                EventType: string.Empty,
                ProviderSessionId: null,
                TransactionRef: null,
                RawPayload: rawPayload));
        }
    }


    private static (string EventType, string? SessionId, string? TxRef) MapStripeEvent(
        Event stripeEvent)
    {
        return stripeEvent.Type switch
        {
            EventTypes.CheckoutSessionCompleted =>
                MapSessionEvent(stripeEvent, PaymentEventTypes.PaymentSucceeded),

            EventTypes.CheckoutSessionAsyncPaymentFailed =>
                MapSessionEvent(stripeEvent, PaymentEventTypes.PaymentFailed),

            _ => (stripeEvent.Type, null, null)
        };
    }

    private static (string EventType, string? SessionId, string? TxRef) MapSessionEvent(
        Event stripeEvent, string normalizedType)
    {
        if (stripeEvent.Data.Object is not Session session)
            return (normalizedType, null, null);

        return (normalizedType, session.Id, session.PaymentIntentId);
    }
}