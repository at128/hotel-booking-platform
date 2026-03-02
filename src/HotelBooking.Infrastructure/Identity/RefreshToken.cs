namespace HotelBooking.Infrastructure.Identity;

public sealed class RefreshToken
{
    private RefreshToken() { } // EF constructor

    public RefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        string family,
        DateTimeOffset expiresAt,
        string? deviceInfo = null)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        Family = family;
        ExpiresAt = expiresAt;
        DeviceInfo = deviceInfo;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = default!; // SHA-256 hash
    public string Family { get; private set; } = default!;    // session family

    public bool IsRevoked { get; private set; }
    public bool IsUsed { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public string? DeviceInfo { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsActive => !IsRevoked && !IsUsed && DateTimeOffset.UtcNow < ExpiresAt;

    public void MarkAsUsed(string replacedByTokenHash)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot use an inactive token.");

        IsUsed = true;
        ReplacedByTokenHash = replacedByTokenHash;
    }

    public void Revoke()
    {
        if (IsRevoked) return;

        IsRevoked = true;
        RevokedAt = DateTimeOffset.UtcNow;
    }
}