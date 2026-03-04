using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Hotels;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Rooms.Command.CreateRoom;

public sealed class CreateRoomCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateRoomCommand, Result<RoomDto>>
{
    public async Task<Result<RoomDto>> Handle(CreateRoomCommand cmd, CancellationToken ct)
    {
        var hotel = await db.Hotels
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == cmd.HotelId, ct);

        if (hotel is null)
            return AdminErrors.Hotels.NotFound;

        var roomType = await db.RoomTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Id == cmd.RoomTypeId, ct);

        if (roomType is null)
            return AdminErrors.Rooms.ReferencedRoomTypeNotFound(cmd.RoomTypeId);

        var exists = await db.HotelRoomTypes
            .AsNoTracking()
            .AnyAsync(x => x.HotelId == cmd.HotelId && x.RoomTypeId == cmd.RoomTypeId, ct);

        if (exists)
            return AdminErrors.Rooms.AlreadyExists;

        var entity = new HotelRoomType(
            id: Guid.NewGuid(),
            hotelId: cmd.HotelId,
            roomTypeId: cmd.RoomTypeId,
            pricePerNight: cmd.PricePerNight,
            adultCapacity: cmd.AdultCapacity,
            childCapacity: cmd.ChildCapacity,
            description: string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim());

        await db.HotelRoomTypes.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);

        return new RoomDto(
            entity.Id,
            entity.HotelId,
            hotel.Name,
            entity.RoomTypeId,
            roomType.Name,
            entity.PricePerNight,
            entity.AdultCapacity,
            entity.ChildCapacity,
            entity.MaxOccupancy,
            entity.Description,
            0,
            entity.CreatedAtUtc,
            entity.LastModifiedUtc);
    }
}