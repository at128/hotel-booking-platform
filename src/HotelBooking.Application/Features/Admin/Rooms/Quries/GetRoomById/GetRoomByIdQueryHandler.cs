using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Rooms.Quries.GetRoomById;

public sealed class GetRoomByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetRoomByIdQuery, Result<RoomDto>>
{
    public async Task<Result<RoomDto>> Handle(GetRoomByIdQuery q, CancellationToken ct)
    {
        var item = await db.Rooms
            .AsNoTracking()
            .Include(x => x.Hotel)
            .Include(x => x.HotelRoomType)
                .ThenInclude(x => x.RoomType)
            .Where(x => x.Id == q.Id && x.DeletedAtUtc == null)
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
            .FirstOrDefaultAsync(ct);

        if (item is null)
            return AdminErrors.Rooms.NotFound(q.Id);

        return item;
    }
}