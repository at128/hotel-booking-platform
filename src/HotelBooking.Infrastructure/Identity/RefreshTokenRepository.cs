using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Identity;

public sealed class RefreshTokenRepository(AppDbContext context) : IRefreshTokenRepository
{
    public async Task<RefreshTokenData?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
    {
        var token = await context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        return token is null ? null : MapToData(token);
    }

    public Task AddAsync(RefreshTokenData data, CancellationToken ct = default)
    {
        var entity = new RefreshToken(
            id: data.Id,
            userId: data.UserId,
            tokenHash: data.TokenHash,
            family: data.Family,
            expiresAt: data.ExpiresAt,
            deviceInfo: data.DeviceInfo);

        context.RefreshTokens.Add(entity);
        return Task.CompletedTask;
    }

    public async Task RevokeAllFamilyAsync(string family, CancellationToken ct = default)
    {
        await context.RefreshTokens
            .Where(t => t.Family == family && !t.IsRevoked)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.IsRevoked, true)
                .SetProperty(t => t.RevokedAt, DateTimeOffset.UtcNow), ct);
    }

    public async Task<bool> TryMarkAsUsedAsync(
        Guid tokenId,
        string replacedByTokenHash,
        DateTimeOffset nowUtc,
        CancellationToken ct = default)
    {
        var affected = await context.RefreshTokens
            .Where(t =>
                t.Id == tokenId &&
                !t.IsUsed &&
                !t.IsRevoked &&
                t.ExpiresAt > nowUtc)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.IsUsed, true)
                .SetProperty(t => t.ReplacedByTokenHash, replacedByTokenHash), ct);

        return affected == 1;
    }

    public async Task RemoveExpiredAsync(CancellationToken ct = default)
    {
        //audit for last 30 days
        var cutoff = DateTimeOffset.UtcNow.AddDays(-30); 
        await context.RefreshTokens
            .Where(t => t.ExpiresAt < cutoff)
            .ExecuteDeleteAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => context.SaveChangesAsync(ct);

    private static RefreshTokenData MapToData(RefreshToken t) => new(
        Id: t.Id,
        UserId: t.UserId,
        TokenHash: t.TokenHash,
        Family: t.Family,
        IsActive: t.IsActive,
        IsUsed: t.IsUsed,
        IsRevoked: t.IsRevoked,
        ExpiresAt: t.ExpiresAt,
        DeviceInfo: t.DeviceInfo);
}