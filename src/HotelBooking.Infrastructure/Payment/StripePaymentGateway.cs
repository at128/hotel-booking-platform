using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models.Payment;
using HotelBooking.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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
                        UnitAmount = ToStripeMinorUnits(request.AmountInUsd),
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
            ,
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    ["booking_id"] = request.BookingId.ToString(),
                    ["booking_number"] = request.BookingNumber
                }
            }
        };

        var requestOptions = new RequestOptions
        {
            IdempotencyKey = $"checkout-session:{request.BookingId}"
        };

        var service = new SessionService();
        var session = await service.CreateAsync(sessionOptions, requestOptions, ct);


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
        // Defensive guards for invalid webhook input (not server misconfiguration)
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            logger.LogWarning("Stripe webhook payload is empty");
            return Task.FromResult(InvalidWebhook(rawPayload));
        }

        if (string.IsNullOrWhiteSpace(signature))
        {
            logger.LogWarning("Stripe webhook signature header is empty");
            return Task.FromResult(InvalidWebhook(rawPayload));
        }

        // This is a server configuration issue, not an invalid webhook request.
        if (string.IsNullOrWhiteSpace(_settings.WebhookSecret))
        {
            throw new InvalidOperationException("Stripe webhook secret is not configured.");
        }

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
            logger.LogWarning(ex, "Stripe webhook signature/payload validation failed");

            return Task.FromResult(InvalidWebhook(rawPayload));
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Stripe webhook payload JSON is malformed");

            return Task.FromResult(InvalidWebhook(rawPayload));
        }
    }

    private static WebhookParseResult InvalidWebhook(string rawPayload) => new(
        IsSignatureValid: false,
        EventType: string.Empty,
        ProviderSessionId: null,
        TransactionRef: null,
        RawPayload: rawPayload);


    private static (string EventType, string? SessionId, string? TxRef) MapStripeEvent(
        Event stripeEvent)
    {
        return stripeEvent.Type switch
        {
            EventTypes.CheckoutSessionCompleted =>
                MapSessionEvent(stripeEvent, PaymentEventTypes.PaymentSucceeded),

            EventTypes.CheckoutSessionAsyncPaymentFailed =>
                MapSessionEvent(stripeEvent, PaymentEventTypes.PaymentFailed),

            EventTypes.PaymentIntentPaymentFailed =>
                MapPaymentIntentEvent(stripeEvent, PaymentEventTypes.PaymentFailed),

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

    private static (string EventType, string? SessionId, string? TxRef) MapPaymentIntentEvent(
    Event stripeEvent, string normalizedType)
    {
        if (stripeEvent.Data.Object is not PaymentIntent paymentIntent)
            return (normalizedType, null, null);

        return (normalizedType, null, paymentIntent.Id);
    }


    public async Task ExpirePaymentSessionAsync(string sessionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session id cannot be empty.", nameof(sessionId));

        var service = new SessionService();

        try
        {
            await service.ExpireAsync(sessionId, cancellationToken: ct);

            logger.LogInformation(
                "Stripe session {SessionId} expired successfully as compensation",
                sessionId);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(
                ex,
                "Failed to expire Stripe session {SessionId} during compensation",
                sessionId);

            throw;
        }
    }

    private static long ToStripeMinorUnits(decimal amount)
    {
        // Defensive rounding to 2 decimals, then convert to integer cents
        var rounded = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        return checked((long)(rounded * 100m));
    }

}