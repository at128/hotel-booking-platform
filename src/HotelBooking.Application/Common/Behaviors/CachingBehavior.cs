using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Common.Behaviors;

public class CachingBehavior<TRequest, TResponse>(
    IMemoryCache cache,
    ILogger<CachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICachedQuery
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (cache.TryGetValue(request.CacheKey, out TResponse? cached))
        {
            logger.LogDebug("Cache hit: {CacheKey}", request.CacheKey);
            return cached!;
        }

        var response = await next();

        if (response is IResult result && result.IsSuccess)
        {
            cache.Set(request.CacheKey, response, request.Expiration);
            logger.LogDebug("Cache set: {CacheKey} ({Expiration})", request.CacheKey, request.Expiration);
        }

        return response;
    }
}
