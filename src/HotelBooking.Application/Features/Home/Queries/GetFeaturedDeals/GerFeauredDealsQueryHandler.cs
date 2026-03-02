using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Home;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Home.Queries.GetFeaturedDeals;

public sealed class GetFeaturedDealsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetFeaturedDealsQuery, Result<FeaturedDealsResponse>>
{
    public async Task<Result<FeaturedDealsResponse>> Handle(
        GetFeaturedDealsQuery request, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var deals = await context.FeaturedDeals
            .Include(fd => fd.Hotel).ThenInclude(h => h.City)
            .Include(fd => fd.HotelRoomType).ThenInclude(hrt => hrt.RoomType)
            .Where(fd =>
                (fd.StartsAtUtc == null || now >= fd.StartsAtUtc) &&
                (fd.EndsAtUtc == null || now <= fd.EndsAtUtc))
            .OrderBy(fd => fd.DisplayOrder)
            .Select(fd => new FeaturedDealDto(
                fd.Id,
                fd.HotelId,
                fd.Hotel.Name,
                fd.Hotel.City.Name,
                fd.Hotel.StarRating,
                fd.Hotel.ThumbnailUrl,
                fd.OriginalPrice,
                fd.DiscountedPrice,
                fd.OriginalPrice > 0
                    ? (int)Math.Round((1 - fd.DiscountedPrice / fd.OriginalPrice) * 100)
                    : 0))
            .ToListAsync(ct);

        return new FeaturedDealsResponse(deals);
    }
}