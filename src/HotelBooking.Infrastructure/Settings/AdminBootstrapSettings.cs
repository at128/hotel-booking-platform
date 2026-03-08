namespace HotelBooking.Infrastructure.Settings;

public sealed class AdminBootstrapSettings
{
    public const string SectionName = "AdminBootstrap";

    public bool Enabled { get; set; } = false;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = "System";
    public string LastName { get; set; } = "Admin";
    public bool EmailConfirmed { get; set; } = true;
}
