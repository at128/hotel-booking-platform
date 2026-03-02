namespace HotelBooking.Application.Common.Settings;

public class BookingSettings
{
    public int CheckoutHoldMinutes { get; set; } = 10;
    public decimal TaxRate { get; set; } = 0.15m;
    public int CancellationFreeHours { get; set; } = 24;
    public decimal CancellationFeePercent { get; set; } = 0.30m;
    public int MaxAdvanceBookingDays { get; set; } = 365;
}