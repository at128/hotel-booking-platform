using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HotelBooking.Infrastructure.Identity;

public sealed class RefreshTokenRepository(AppDbContext context) : IRefreshTokenRepository
{
    public async Task<RefreshTokenData?> GetByHashAsync(
        string tokenHash, CancellationToken ct = default)
    {
        var token = await context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        return token is null ? null : MapToData(token);
    }

    public async Task AddAsync(RefreshTokenData data, CancellationToken ct = default)
    {
        var entity = new RefreshToken(
            id: data.Id,
            userId: data.UserId,
            tokenHash: data.TokenHash,
            family: data.Family,
            expiresAt: data.ExpiresAt,
            deviceInfo: data.DeviceInfo);

        context.RefreshTokens.Add(entity);
        await context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Atomically:
    /// 1. Marks the old token as used (only if it's still active)
    /// 2. Inserts the new token
    /// Returns false if old token was already used → reuse attack detected.
    /// </summary>
    public async Task<bool> RotateAsync(
        Guid oldTokenId,
        string oldTokenFamily,
        RefreshTokenData newToken,
        DateTimeOffset nowUtc,
        CancellationToken ct = default)
    {
        await using IDbContextTransaction tx =
            await context.Database.BeginTransactionAsync(ct);

        try
        {
            var affected = await context.RefreshTokens
                .Where(t =>
                    t.Id == oldTokenId &&
                    !t.IsUsed &&
                    !t.IsRevoked &&
                    t.ExpiresAt > nowUtc)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.IsUsed, true)
                    .SetProperty(t => t.ReplacedByTokenHash, newToken.TokenHash),
                    ct);

            if (affected == 0)
            {
                await tx.RollbackAsync(ct);
                return false;
            }

            var newEntity = new RefreshToken(
                id: newToken.Id,
                userId: newToken.UserId,
                tokenHash: newToken.TokenHash,
                family: newToken.Family,      // Same family = same session
                expiresAt: newToken.ExpiresAt,
                deviceInfo: newToken.DeviceInfo);

            context.RefreshTokens.Add(newEntity);
            await context.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
            return true;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task RevokeAllFamilyAsync(
        string family, CancellationToken ct = default)
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
        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
        await context.RefreshTokens
            .Where(t => t.ExpiresAt < cutoff)
            .ExecuteDeleteAsync(ct);
    }

    public async Task RevokeAllForUserAsync(
        Guid userId, CancellationToken ct = default)
    {
        await context.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.IsRevoked, true)
                .SetProperty(t => t.RevokedAt, DateTimeOffset.UtcNow), ct);
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