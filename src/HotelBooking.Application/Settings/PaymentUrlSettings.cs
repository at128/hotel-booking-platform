namespace HotelBooking.Application.Settings;

/// <summary>
/// {0} = BookingId placeholder
/// For Example "https://yourapp.com/booking/{0}/success"
/// </summary>
public sealed class PaymentUrlSettings
{
    public const string SectionName = "PaymentUrls";

    public string SuccessUrlTemplate { get; init; } = string.Empty;
    public string CancelUrlTemplate { get; init; } = string.Empty;
}