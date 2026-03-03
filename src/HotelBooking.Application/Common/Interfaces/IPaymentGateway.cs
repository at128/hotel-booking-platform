using HotelBooking.Application.Common.Models.Payment;
using System.Threading.Channels;

namespace HotelBooking.Application.Common.Interfaces;

public interface IPaymentGateway
{
    // فائدة ProviderName: للـ logging فقط
    // بكرا لما تشوف في الـ logs "Stripe" أو "PayPal" تعرف مين تصرّف
    string ProviderName { get; }

    Task<PaymentSessionResponse> CreatePaymentSessionAsync(
        PaymentSessionRequest request,
        CancellationToken ct = default);

    Task<WebhookParseResult> ParseWebhookAsync(
        string rawPayload,
        string signature,
        CancellationToken ct = default);
}