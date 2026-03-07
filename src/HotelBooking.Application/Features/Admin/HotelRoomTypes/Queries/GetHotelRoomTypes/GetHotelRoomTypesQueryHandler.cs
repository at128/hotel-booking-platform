using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin.HotelRoomTypes;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.HotelRoomTypes.Queries.GetHotelRoomTypes;

public sealed class GetHotelRoomTypesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetHotelRoomTypesQuery, Result<List<HotelRoomTypeAdminDto>>>
{
    public async Task<Result<List<HotelRoomTypeAdminDto>>> Handle(
        GetHotelRoomTypesQuery query, CancellationToken ct)
    {
        var items = await db.HotelRoomTypes
            .AsNoTracking()
            .Where(x => x.DeletedAtUtc == null)
            .Where(x => !query.HotelId.HasValue || x.HotelId == query.HotelId.Value)
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
            .ToListAsync(ct);

        return items;
    }
}