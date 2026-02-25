using HotelBooking.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;

namespace HotelBooking.Infrastructure.Caching;

public sealed class CacheInvalidator(HybridCache cache) : ICacheInvalidator
{
    public async Task RemoveAsync(string key, CancellationToken ct = default)
        => await cache.RemoveAsync(key, ct);

    public async Task RemoveByTagAsync(string tag, CancellationToken ct = default)
        => await cache.RemoveByTagAsync(tag, ct);

    public async Task RemoveByTagsAsync(IEnumerable<string> tags, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(tags);

        var cleaned = tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (cleaned.Length == 0)
            return;

        await Task.WhenAll(cleaned.Select(t => cache.RemoveByTagAsync(t, ct).AsTask()));
    }
}