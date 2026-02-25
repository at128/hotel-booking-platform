using System.Text.RegularExpressions;
using Serilog.Context;

namespace HotelBooking.Api.Infrastructure;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string Header = "X-Correlation-Id";
    private const int MaxLength = 64;

    private static readonly Regex SafeCorrelationIdRegex =
        new("^[a-zA-Z0-9-]{1,64}$", RegexOptions.Compiled);

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId;

        if (!context.Request.Headers.TryGetValue(Header, out var rawValue) ||
            string.IsNullOrWhiteSpace(rawValue))
        {
            correlationId = Guid.NewGuid().ToString();
        }
        else
        {
            var candidate = rawValue.ToString().Trim();

            correlationId = candidate.Length <= MaxLength &&
                            SafeCorrelationIdRegex.IsMatch(candidate)
                ? candidate
                : Guid.NewGuid().ToString();
        }

        context.Response.Headers[Header] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
            await next(context);
    }
}