namespace HotelBooking.Api.BackgroundJobs;

public sealed class ExpirePendingPaymentsJobSettings
{
    public const string SectionName = "Jobs:ExpirePendingPayments";

    public bool Enabled { get; init; } = true;

    public int IntervalSeconds { get; init; } = 60;

    public int BatchSize { get; init; } = 100;
}