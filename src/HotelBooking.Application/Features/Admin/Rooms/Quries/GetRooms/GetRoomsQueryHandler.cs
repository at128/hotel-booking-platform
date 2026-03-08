using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Rooms.Quries.GetRooms;

public sealed class GetRoomsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetRoomsQuery, Result<PaginatedAdminResponse<RoomDto>>>
{
    public async Task<Result<PaginatedAdminResponse<RoomDto>>> Handle(
        GetRoomsQuery q,
        CancellationToken ct)
    {
        var pageSize = Math.Clamp(q.PageSize, 1, 100);
        var page = Math.Max(1, q.Page);

        var query = db.Rooms
            .AsNoTracking()
            .Include(x => x.Hotel)
            .Include(x => x.HotelRoomType)
                .ThenInclude(x => x.RoomType)
            .Where(x => x.DeletedAtUtc == null)
            .AsQueryable();

        if (q.HotelId.HasValue)
            query = query.Where(x => x.HotelId == q.HotelId.Value);

        if (q.RoomTypeId.HasValue)
            query = query.Where(x => x.HotelRoomType.RoomTypeId == q.RoomTypeId.Value);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim().ToLower();

            query = query.Where(x =>
                x.Hotel.Name.ToLower().Contains(term) ||
                x.HotelRoomType.RoomType.Name.ToLower().Contains(term) ||
                x.RoomNumber.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(x => x.Hotel.Name)
            .ThenBy(x => x.RoomNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new RoomDto(
                x.Id,
                x.HotelId,
                x.Hotel.Name,
                x.HotelRoomTypeId,
                x.HotelRoomType.RoomType.Name,
                x.RoomNumber,
                x.Floor,
                x.Status,
                x.CreatedAtUtc,
                x.LastModifiedUtc))
            .ToListAsync(ct);

        return new PaginatedAdminResponse<RoomDto>(
            Items: items,
            TotalCount: total,
            Page: page,
            PageSize: pageSize,
            HasMore: (page * pageSize) < total);
    }
}