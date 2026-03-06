using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Rooms;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Rooms.Commands.CreateRoom;

public sealed class CreateRoomCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateRoomCommand, Result<RoomDto>>
{
    public async Task<Result<RoomDto>> Handle(CreateRoomCommand cmd, CancellationToken ct)
    {
        var roomNumber = cmd.RoomNumber.Trim();

        var hotelRoomType = await db.HotelRoomTypes
            .Include(x => x.Hotel)
            .Include(x => x.RoomType)
            .FirstOrDefaultAsync(x => x.Id == cmd.HotelRoomTypeId && x.DeletedAtUtc == null, ct);

        if (hotelRoomType is null)
            return AdminErrors.Rooms.ReferencedHotelRoomTypeNotFound(cmd.HotelRoomTypeId);

        var exists = await db.Rooms
            .AsNoTracking()
            .AnyAsync(r =>
                r.HotelId == hotelRoomType.HotelId &&
                r.RoomNumber == roomNumber &&
                r.DeletedAtUtc == null, ct);

        if (exists)
            return AdminErrors.Rooms.DuplicateRoomNumber;

        var entity = new Room(
            id: Guid.CreateVersion7(),
            hotelRoomTypeId: hotelRoomType.Id,
            hotelId: hotelRoomType.HotelId,
            roomNumber: roomNumber,
            floor: cmd.Floor,
            status: cmd.Status);

        db.Rooms.Add(entity);
        await db.SaveChangesAsync(ct);

        return new RoomDto(
            Id: entity.Id,
            HotelId: entity.HotelId,
            HotelName: hotelRoomType.Hotel.Name,
            HotelRoomTypeId: entity.HotelRoomTypeId,
            RoomTypeName: hotelRoomType.RoomType.Name,
            RoomNumber: entity.RoomNumber,
            Floor: entity.Floor,
            Status: entity.Status,
            CreatedAtUtc: entity.CreatedAtUtc,
            LastModifiedUtc: entity.LastModifiedUtc);
    }
}