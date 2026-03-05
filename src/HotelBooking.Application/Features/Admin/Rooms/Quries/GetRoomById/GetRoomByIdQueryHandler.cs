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
        var item = await db.HotelRoomTypes
            .AsNoTracking()
            .Include(x => x.Hotel)
            .Include(x => x.RoomType)
            .Include(x => x.Rooms)
            .Where(x => x.Id == q.Id)
            .Select(x => new RoomDto(
                x.Id,
                x.HotelId,
                x.Hotel.Name,
                x.RoomTypeId,
                x.RoomType.Name,
                x.PricePerNight,
                x.AdultCapacity,
                x.ChildCapacity,
                x.MaxOccupancy,
                x.Description,
                x.Rooms.Count(r => r.DeletedAtUtc == null),
                x.CreatedAtUtc,
                x.LastModifiedUtc))
            .FirstOrDefaultAsync(ct);

        if (item is null)
            return AdminErrors.Rooms.NotFound(q.Id);

        return item;
    }
}