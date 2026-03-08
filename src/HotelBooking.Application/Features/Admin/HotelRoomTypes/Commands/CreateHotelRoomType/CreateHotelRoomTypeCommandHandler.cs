using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Hotels;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.HotelRoomTypes.Commands.CreateHotelRoomType;

public sealed class CreateHotelRoomTypeCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateHotelRoomTypeCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateHotelRoomTypeCommand cmd, CancellationToken ct)
    {
        var hotelExists = await db.Hotels
            .AnyAsync(h => h.Id == cmd.HotelId && h.DeletedAtUtc == null, ct);

        if (!hotelExists)
            return AdminErrors.Hotels.NotFound;

        var roomTypeExists = await db.RoomTypes
            .AnyAsync(rt => rt.Id == cmd.RoomTypeId && rt.DeletedAtUtc == null, ct);

        if (!roomTypeExists)
            return AdminErrors.RoomTypes.NotFound(cmd.RoomTypeId);

        var alreadyExists = await db.HotelRoomTypes
            .AnyAsync(x =>
                x.HotelId == cmd.HotelId &&
                x.RoomTypeId == cmd.RoomTypeId &&
                x.DeletedAtUtc == null, ct);

        if (alreadyExists)
            return AdminErrors.HotelRoomTypes.AlreadyExists;

        var entity = new HotelRoomType(
            id: Guid.CreateVersion7(),
            hotelId: cmd.HotelId,
            roomTypeId: cmd.RoomTypeId,
            pricePerNight: cmd.PricePerNight,
            adultCapacity: cmd.AdultCapacity,
            childCapacity: cmd.ChildCapacity,
            description: cmd.Description,
            maxOccupancy: cmd.MaxOccupancy);

        db.HotelRoomTypes.Add(entity);
        await db.SaveChangesAsync(ct);

        return entity.Id;
    }
}