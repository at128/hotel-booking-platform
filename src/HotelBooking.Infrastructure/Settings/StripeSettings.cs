namespace HotelBooking.Infrastructure.Settings;

public sealed class StripeSettings
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; init; } = string.Empty;
    public string WebhookSecret { get; init; } = string.Empty;

    public string Currency { get; init; } = "usd";
}