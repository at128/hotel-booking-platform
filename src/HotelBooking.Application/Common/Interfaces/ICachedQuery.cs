using MediatR;

namespace HotelBooking.Application.Common.Interfaces;

public interface ICachedQuery
{
    string CacheKey { get; }
    string[] Tags { get; }
    TimeSpan Expiration { get; }
}

public interface ICachedQuery<out TResponse> : IRequest<TResponse>, ICachedQuery;