using System.Text.RegularExpressions;
using HotelBooking.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace HotelBooking.Api.Infrastructure;

internal sealed partial class CookieSettingsValidator : IValidateOptions<CookieSettings>
{
    public ValidateOptionsResult Validate(string? name, CookieSettings options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.RefreshTokenCookieName))
        {
            failures.Add("CookieSettings:RefreshTokenCookieName is required.");
        }
        else if (!CookieNameRegex().IsMatch(options.RefreshTokenCookieName))
        {
            failures.Add(
                "CookieSettings:RefreshTokenCookieName contains invalid characters.");
        }

        if (options.RefreshTokenExpiryDays is <= 0 or > 90)
        {
            failures.Add(
                "CookieSettings:RefreshTokenExpiryDays must be between 1 and 90.");
        }

        if (string.IsNullOrWhiteSpace(options.SameSite))
        {
            failures.Add("CookieSettings:SameSite is required.");
        }
        else if (!TryParseSameSite(options.SameSite, out var sameSite))
        {
            failures.Add(
                "CookieSettings:SameSite must be one of: Strict, Lax, None.");
        }
        else if (sameSite == SameSiteMode.None && !options.SecureOnly)
        {
            failures.Add(
                "CookieSettings:SecureOnly must be true when SameSite is None.");
        }

        if (string.IsNullOrWhiteSpace(options.Path))
        {
            failures.Add("CookieSettings:Path is required.");
        }
        else if (!options.Path.StartsWith("/", StringComparison.Ordinal))
        {
            failures.Add("CookieSettings:Path must start with '/'.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool TryParseSameSite(string value, out SameSiteMode mode)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "strict":
                mode = SameSiteMode.Strict;
                return true;
            case "lax":
                mode = SameSiteMode.Lax;
                return true;
            case "none":
                mode = SameSiteMode.None;
                return true;
            default:
                mode = default;
                return false;
        }
    }

    [GeneratedRegex("^[!#$%&'*+.^_`|~0-9A-Za-z-]{1,64}$", RegexOptions.Compiled)]
    private static partial Regex CookieNameRegex();
}
