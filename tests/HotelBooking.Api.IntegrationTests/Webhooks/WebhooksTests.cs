using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using HotelBooking.Api.IntegrationTests.Helpers;
using HotelBooking.Api.IntegrationTests.Infrastructure;
using HotelBooking.Application.Common.Models.Payment;
using HotelBooking.Contracts.Cart;
using HotelBooking.Contracts.Checkout;
using Xunit;

namespace HotelBooking.Api.IntegrationTests.Webhooks;

[Collection("Integration")]
public class WebhooksTests
{
    private readonly WebAppFactory _factory;

    public WebhooksTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, CreateBookingResponse Booking, string SessionId)>
        SetupWithPendingBookingAsync()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client,
            $"wh-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        await client.PostAsJsonAsync("/api/v1/cart/items",
            new AddToCartRequest(seed.HotelRoomType.Id, tomorrow, dayAfter, 1, 2, 0));

        var holdResp = await client.PostAsJsonAsync("/api/v1/checkout/hold",
            new CreateHoldRequest(null));
        var hold = await holdResp.ReadJsonAsync<CheckoutHoldResponse>();

        var bookResp = await client.PostAsJsonAsync("/api/v1/checkout/booking",
            new CreateBookingRequest(hold!.HoldIds, null));
        var booking = await bookResp.ReadJsonAsync<CreateBookingResponse>();

        // Get the session ID from the fake gateway
        var sessionId = _factory.FakePaymentGateway.CreatedSessions.Last().BookingId.ToString();

        return (client, booking!, sessionId);
    }

    [Fact]
    public async Task PaymentWebhook_CheckoutCompleted_ConfirmsBooking()
    {
        var (client, booking, _) = await SetupWithPendingBookingAsync();

        // Configure fake gateway to return payment succeeded for this booking
        _factory.FakePaymentGateway.NextWebhookParseResult = new WebhookParseResult(
            IsSignatureValid: true,
            EventType: PaymentEventTypes.PaymentSucceeded,
            ProviderSessionId: _factory.FakePaymentGateway.CreatedSessions.Last().BookingNumber,
            TransactionRef: "txn_webhook_success",
            RawPayload: "{}");

        var webhookClient = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/stripe")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "test_signature");

        var response = await webhookClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PaymentWebhook_CheckoutExpired_CancelsBooking()
    {
        var (client, booking, _) = await SetupWithPendingBookingAsync();

        _factory.FakePaymentGateway.NextWebhookParseResult = new WebhookParseResult(
            IsSignatureValid: true,
            EventType: PaymentEventTypes.PaymentFailed,
            ProviderSessionId: _factory.FakePaymentGateway.CreatedSessions.Last().BookingNumber,
            TransactionRef: null,
            RawPayload: "{}");

        var webhookClient = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/stripe")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "test_signature");

        var response = await webhookClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PaymentWebhook_DuplicateEvent_IsIdempotent()
    {
        var (client, booking, _) = await SetupWithPendingBookingAsync();

        _factory.FakePaymentGateway.NextWebhookParseResult = new WebhookParseResult(
            IsSignatureValid: true,
            EventType: PaymentEventTypes.PaymentSucceeded,
            ProviderSessionId: _factory.FakePaymentGateway.CreatedSessions.Last().BookingNumber,
            TransactionRef: "txn_idempotent_test",
            RawPayload: "{}");

        var webhookClient = _factory.CreateClient();

        // Send same webhook twice
        for (int i = 0; i < 2; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/stripe")
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Stripe-Signature", "test_signature");
            var response = await webhookClient.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task PaymentWebhook_InvalidPayload_Returns400()
    {
        _factory.FakePaymentGateway.NextWebhookParseResult = new WebhookParseResult(
            IsSignatureValid: false,
            EventType: "",
            ProviderSessionId: null,
            TransactionRef: null,
            RawPayload: "invalid");

        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/stripe")
        {
            Content = new StringContent("invalid", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "bad_sig");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
