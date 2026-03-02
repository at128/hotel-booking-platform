namespace HotelBooking.Api.Infrastructure;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

        // CSP — API-specific: no HTML rendering, block everything non-essential
        headers["Content-Security-Policy"] =
            "default-src 'none'; frame-ancestors 'none'";

        // Prevent caching of sensitive responses by default
        if (!context.Response.Headers.ContainsKey("Cache-Control"))
            headers["Cache-Control"] = "no-store";

        await next(context);
    }
}