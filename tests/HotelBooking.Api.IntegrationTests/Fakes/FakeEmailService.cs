using HotelBooking.Application.Common.Interfaces;

namespace HotelBooking.Api.IntegrationTests.Fakes;
using Xunit;

public class FakeEmailService : IEmailService
{
    public List<SentEmail> SentEmails { get; } = [];

    public Task SendBookingConfirmationAsync(
        string toEmail,
        BookingConfirmationEmailData data,
        CancellationToken ct = default)
    {
        SentEmails.Add(new SentEmail(toEmail, data));
        return Task.CompletedTask;
    }

    public void Reset() => SentEmails.Clear();

    public record SentEmail(string ToEmail, BookingConfirmationEmailData Data);
}
