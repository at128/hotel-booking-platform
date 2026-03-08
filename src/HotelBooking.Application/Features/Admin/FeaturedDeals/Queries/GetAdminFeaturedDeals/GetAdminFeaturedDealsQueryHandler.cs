using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Contracts.Common;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.FeaturedDeals.Queries.GetAdminFeaturedDeals;

public sealed class GetAdminFeaturedDealsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetAdminFeaturedDealsQuery, Result<PaginatedResponse<FeaturedDealDto>>>
{
    public async Task<Result<PaginatedResponse<FeaturedDealDto>>> Handle(
        GetAdminFeaturedDealsQuery query, CancellationToken ct)
    {
        var q = db.FeaturedDeals
            .Include(d => d.Hotel)
            .Include(d => d.HotelRoomType)
                .ThenInclude(hrt => hrt.RoomType)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(d => d.Hotel.Name.ToLower().Contains(s));
        }

        q = q.OrderBy(d => d.DisplayOrder).ThenByDescending(d => d.CreatedAtUtc);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(d => new FeaturedDealDto(
                d.Id, d.HotelId, d.Hotel.Name,
                d.HotelRoomTypeId, d.OriginalPrice,
                d.DiscountedPrice, d.DisplayOrder,
                d.StartsAtUtc, d.EndsAtUtc,
                d.IsActive()))
            .ToListAsync(ct);

        var hasMore = query.Page * query.PageSize < total;

        return new PaginatedResponse<FeaturedDealDto>(
            items,
            total,
            query.Page,
            query.PageSize,
            hasMore);
    }
}