using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin.HotelRoomTypes;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.HotelRoomTypes.Queries.GetHotelRoomTypeById;

public sealed class GetHotelRoomTypeByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetHotelRoomTypeByIdQuery, Result<HotelRoomTypeAdminDto>>
{
    public async Task<Result<HotelRoomTypeAdminDto>> Handle(
        GetHotelRoomTypeByIdQuery query, CancellationToken ct)
    {
        var item = await db.HotelRoomTypes
            .AsNoTracking()
            .Where(x => x.Id == query.Id && x.DeletedAtUtc == null)
            .Select(x => new HotelRoomTypeAdminDto(
                x.Id,
                x.HotelId,
                x.Hotel.Name,
                x.RoomTypeId,
                x.RoomType.Name,
                x.PricePerNight,
                x.AdultCapacity,
                x.ChildCapacity,
                x.MaxOccupancy,
                x.Description))
            .FirstOrDefaultAsync(ct);

        if (item is null)
            return AdminErrors.HotelRoomTypes.NotFound(query.Id);

        return item;
    }
}