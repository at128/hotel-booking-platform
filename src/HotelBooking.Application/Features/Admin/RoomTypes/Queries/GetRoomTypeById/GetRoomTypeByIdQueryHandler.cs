using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Queries.GetRoomTypeById;

public sealed class GetRoomTypeByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetRoomTypeByIdQuery, Result<RoomTypeDto>>
{
    public async Task<Result<RoomTypeDto>> Handle(GetRoomTypeByIdQuery q, CancellationToken ct)
    {
        var item = await db.RoomTypes
            .AsNoTracking()
            .Where(rt => rt.Id == q.Id && rt.DeletedAtUtc == null)
            .Select(rt => new RoomTypeDto(
                rt.Id,
                rt.Name,
                rt.Description,
                rt.HotelRoomTypes.Count(x => x.DeletedAtUtc == null),
                rt.CreatedAtUtc,
                rt.LastModifiedUtc))
            .FirstOrDefaultAsync(ct);

        if (item is null)
            return AdminErrors.RoomTypes.NotFound(q.Id);

        return item;
    }
}