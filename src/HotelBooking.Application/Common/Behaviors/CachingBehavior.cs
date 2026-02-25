using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Common.Behaviors;

public class CachingBehavior<TRequest, TResponse>(
    HybridCache cache,
    ILogger<CachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICachedQuery
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        return await cache.GetOrCreateAsync(
            key: request.CacheKey,
            factory: async cancel =>
            {
                var response = await next();

                if (response is IResult result && !result.IsSuccess)
                {
                    logger.LogDebug("Cache skip (failed result): {CacheKey}", request.CacheKey);
                }
                else
                {
                    logger.LogDebug("Cache set: {CacheKey} ({Expiration})", request.CacheKey, request.Expiration);
                }

                return response;
            },
            options: new HybridCacheEntryOptions
            {
                Expiration = request.Expiration
            },
            tags: request.Tags,
            cancellationToken: ct);
    }
}