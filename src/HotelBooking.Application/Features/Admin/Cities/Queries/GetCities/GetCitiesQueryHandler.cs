using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Cities.Queries.GetCities;

public sealed class GetCitiesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetCitiesQuery, Result<PaginatedAdminResponse<CityDto>>>
{
    public async Task<Result<PaginatedAdminResponse<CityDto>>> Handle(
        GetCitiesQuery q, CancellationToken ct)
    {
        var pageSize = Math.Clamp(q.PageSize, 1, 100);
        var page = Math.Max(1, q.Page);

        var query = db.Cities
            .AsNoTracking()
            .Where(c => c.DeletedAtUtc == null);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim().ToLower();

            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                c.Country.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CityDto(
                c.Id,
                c.Name,
                c.Country,
                c.PostOffice,
                c.Hotels.Count(h => h.DeletedAtUtc == null),
                c.CreatedAtUtc,
                c.LastModifiedUtc)) 
            .ToListAsync(ct);

        return new PaginatedAdminResponse<CityDto>(
            items,
            total,
            page,
            pageSize,
            page * pageSize < total);
    }
}