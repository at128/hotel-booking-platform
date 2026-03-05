using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Infrastructure.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace HotelBooking.Infrastructure.Email;

internal sealed class SmtpEmailService(
    IOptions<EmailSettings> options,
    ILogger<SmtpEmailService> logger)
    : IEmailService
{
    private readonly EmailSettings _settings = options.Value;

    public async Task SendBookingConfirmationAsync(
        string toEmail,
        BookingConfirmationEmailData data,
        CancellationToken ct = default)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = $"Booking Confirmed — {data.BookingNumber}";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = BuildHtmlBody(data),
                TextBody = BuildTextBody(data)
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _settings.SmtpHost,
                _settings.SmtpPort,
                _settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                ct);

            await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPassword, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            logger.LogInformation(
                "Booking confirmation email sent for {BookingNumber} to {Email}",
                data.BookingNumber, toEmail);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Email failure should NOT fail the webhook acknowledgement.
            // Log and swallow so Stripe gets 200 and doesn't retry.
            logger.LogError(ex,
                "Failed to send booking confirmation email for {BookingNumber}",
                data.BookingNumber);
        }
    }

    private static string BuildHtmlBody(BookingConfirmationEmailData d)
    {
        var rows = string.Join("", d.Rooms.Select(r =>
            $"<tr><td>{r.RoomTypeName}</td><td>{r.RoomNumber}</td><td>${r.PricePerNight:F2}/night</td></tr>"));

        return $"""
            <html><body style="font-family:Arial,sans-serif">
            <h2>Your booking is confirmed!</h2>
            <p><strong>Booking Number:</strong> {d.BookingNumber}</p>
            <p><strong>Hotel:</strong> {d.HotelName}</p>
            <p><strong>Address:</strong> {d.HotelAddress}</p>
            <p><strong>Check-in:</strong> {d.CheckIn:yyyy-MM-dd} &nbsp;
               <strong>Check-out:</strong> {d.CheckOut:yyyy-MM-dd} ({d.Nights} nights)</p>
            <table border="1" cellpadding="6" cellspacing="0">
              <tr><th>Room Type</th><th>Room Number</th><th>Price</th></tr>
              {rows}
            </table>
            <p><strong>Total Amount:</strong> ${d.TotalAmount:F2}</p>
            <p><strong>Transaction Ref:</strong> {d.TransactionRef}</p>
            <p>Thank you for booking with HotelBooking!</p>
            </body></html>
            """;
    }

    private static string BuildTextBody(BookingConfirmationEmailData d) =>
        $"Booking Confirmed: {d.BookingNumber}\n" +
        $"Hotel: {d.HotelName}\n" +
        $"Check-in: {d.CheckIn:yyyy-MM-dd} / Check-out: {d.CheckOut:yyyy-MM-dd}\n" +
        $"Total: ${d.TotalAmount:F2}\n" +
        $"Transaction: {d.TransactionRef}";
}