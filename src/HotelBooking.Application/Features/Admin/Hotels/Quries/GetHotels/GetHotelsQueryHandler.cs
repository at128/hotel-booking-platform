using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Hotels.Query.GetHotels;

public sealed class GetHotelsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetHotelsQuery, Result<PaginatedAdminResponse<HotelDto>>>
{
    public async Task<Result<PaginatedAdminResponse<HotelDto>>> Handle(
        GetHotelsQuery q, CancellationToken ct)
    {
        var pageSize = Math.Clamp(q.PageSize, 1, 100);
        var page = Math.Max(1, q.Page);

        var query = db.Hotels
            .AsNoTracking()
            .Where(h => h.DeletedAtUtc == null);

        if (q.CityId.HasValue)
        {
            query = query.Where(h => h.CityId == q.CityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim().ToLower();

            query = query.Where(h =>
                h.Name.ToLower().Contains(term) ||
                h.Owner.ToLower().Contains(term) ||
                h.City.Name.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(h => h.Name)
            .ThenBy(h => h.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new HotelDto(
                h.Id,
                h.CityId,
                h.City.Name,
                h.Name,
                h.Owner,
                h.Address,
                h.StarRating,
                h.Description,
                h.Latitude,
                h.Longitude,
                h.MinPricePerNight,
                h.AverageRating,
                h.ReviewCount,
                h.HotelRoomTypes.Count(rt => rt.DeletedAtUtc == null),
                h.CreatedAtUtc,
                h.LastModifiedUtc))
            .ToListAsync(ct);

        return new PaginatedAdminResponse<HotelDto>(
            Items: items,
            TotalCount: total,
            Page: page,
            PageSize: pageSize,
            HasMore: (page * pageSize) < total);
    }
}