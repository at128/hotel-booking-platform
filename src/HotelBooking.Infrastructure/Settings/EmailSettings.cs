using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Infrastructure.Settings;

public sealed class EmailSettings
{
    public const string SectionName = "Email";

    [Required] public string SmtpHost { get; init; } = null!;
    [Range(1, 65535)] public int SmtpPort { get; init; } = 587;
    [Required] public string SmtpUser { get; init; } = null!;
    [Required] public string SmtpPassword { get; init; } = null!;
    [Required] public string FromAddress { get; init; } = null!;
    [Required] public string FromName { get; init; } = "HotelBooking";
    public bool UseSsl { get; init; } = true;
}