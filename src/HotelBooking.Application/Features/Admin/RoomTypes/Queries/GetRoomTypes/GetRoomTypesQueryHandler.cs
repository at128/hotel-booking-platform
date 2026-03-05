using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Queries.GetRoomTypes;

public sealed class GetRoomTypesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetRoomTypesQuery, Result<PaginatedAdminResponse<RoomTypeDto>>>
{
    public async Task<Result<PaginatedAdminResponse<RoomTypeDto>>> Handle(
        GetRoomTypesQuery q,
        CancellationToken ct)
    {
        var pageSize = Math.Clamp(q.PageSize, 1, 100);
        var page = Math.Max(1, q.Page);

        var query = db.RoomTypes
            .AsNoTracking()
            .Where(rt => rt.DeletedAtUtc == null);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim().ToLower();
            query = query.Where(rt => rt.Name.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(rt => rt.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(rt => new RoomTypeDto(
                rt.Id,
                rt.Name,
                rt.Description,
                rt.HotelRoomTypes.Count(x => x.DeletedAtUtc == null),
                rt.CreatedAtUtc,
                rt.LastModifiedUtc))
            .ToListAsync(ct);

        return new PaginatedAdminResponse<RoomTypeDto>(
            items,
            total,
            page,
            pageSize,
            page * pageSize < total);
    }
}