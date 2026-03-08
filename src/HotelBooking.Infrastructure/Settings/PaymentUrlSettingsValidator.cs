using System.Globalization;
using HotelBooking.Application.Settings;
using Microsoft.Extensions.Options;

namespace HotelBooking.Infrastructure.Settings;

internal sealed class PaymentUrlSettingsValidator : IValidateOptions<PaymentUrlSettings>
{
    public ValidateOptionsResult Validate(string? name, PaymentUrlSettings options)
    {
        var failures = new List<string>();

        ValidateTemplate(options.SuccessUrlTemplate, nameof(PaymentUrlSettings.SuccessUrlTemplate), failures);
        ValidateTemplate(options.CancelUrlTemplate, nameof(PaymentUrlSettings.CancelUrlTemplate), failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateTemplate(string? template, string propertyName, List<string> failures)
    {
        var key = $"{PaymentUrlSettings.SectionName}:{propertyName}";

        if (string.IsNullOrWhiteSpace(template))
        {
            failures.Add($"{key} is required.");
            return;
        }

        if (!template.Contains("{0", StringComparison.Ordinal))
        {
            failures.Add($"{key} must include a '{{0}}' booking id placeholder.");
            return;
        }

        string formattedUrl;
        try
        {
            formattedUrl = string.Format(CultureInfo.InvariantCulture, template, Guid.Empty);
        }
        catch (FormatException)
        {
            failures.Add($"{key} is not a valid format template.");
            return;
        }

        if (!Uri.TryCreate(formattedUrl, UriKind.Absolute, out var uri))
        {
            failures.Add($"{key} must produce an absolute URL.");
            return;
        }

        if (!IsAllowedScheme(uri))
        {
            failures.Add(
                $"{key} must use HTTPS. HTTP is allowed only for local loopback addresses.");
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            failures.Add($"{key} must not contain URL user info.");
        }

        if (!string.IsNullOrEmpty(uri.Fragment))
        {
            failures.Add($"{key} must not contain a URL fragment.");
        }
    }

    private static bool IsAllowedScheme(Uri uri)
    {
        if (uri.Scheme == Uri.UriSchemeHttps)
            return true;

        return uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback;
    }
}
