using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Services.Queries.GetServices;

public sealed class GetServicesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetServicesQuery, Result<PaginatedAdminResponse<ServiceDto>>>
{
    public async Task<Result<PaginatedAdminResponse<ServiceDto>>> Handle(
        GetServicesQuery q,
        CancellationToken ct)
    {
        var pageSize = Math.Clamp(q.PageSize, 1, 100);
        var page = Math.Max(1, q.Page);

        var query = db.Services
            .AsNoTracking()
            .Where(s => s.DeletedAtUtc == null); // explicit + clear

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ServiceDto(
                s.Id,
                s.Name,
                s.Description,
                s.HotelServices.Count(),
                s.CreatedAtUtc,
                s.UpdatedAtUtc))
            .ToListAsync(ct);

        return new PaginatedAdminResponse<ServiceDto>(
            items,
            total,
            page,
            pageSize,
            (page * pageSize) < total);
    }
}