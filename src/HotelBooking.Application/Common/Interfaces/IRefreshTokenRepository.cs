namespace HotelBooking.Application.Common.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshTokenData?> GetByHashAsync(string tokenHash, CancellationToken ct = default);

    Task AddAsync(RefreshTokenData token, CancellationToken ct = default);

    Task RevokeAllFamilyAsync(string family, CancellationToken ct = default);

    Task<bool> TryMarkAsUsedAsync(
        Guid tokenId,
        string replacedByTokenHash,
        DateTimeOffset nowUtc,
        CancellationToken ct = default);

    Task RemoveExpiredAsync(CancellationToken ct = default);

    Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public sealed record RefreshTokenData(
    Guid Id,
    Guid UserId,
    string TokenHash,
    string Family,
    bool IsActive,
    bool IsUsed,
    bool IsRevoked,
    DateTimeOffset ExpiresAt,
    string? DeviceInfo = null);