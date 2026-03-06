using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Rooms.Commands.UpdateRoom;

public sealed class UpdateRoomCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateRoomCommand, Result<RoomDto>>
{
    public async Task<Result<RoomDto>> Handle(UpdateRoomCommand cmd, CancellationToken ct)
    {
        var entity = await db.Rooms
            .Include(x => x.Hotel)
            .Include(x => x.HotelRoomType)
                .ThenInclude(x => x.RoomType)
            .FirstOrDefaultAsync(x => x.Id == cmd.Id && x.DeletedAtUtc == null, ct);

        if (entity is null)
            return AdminErrors.Rooms.NotFound(cmd.Id);

        var roomNumber = cmd.RoomNumber.Trim();

        var duplicateExists = await db.Rooms
            .AsNoTracking()
            .AnyAsync(r =>
                r.Id != cmd.Id &&
                r.HotelId == entity.HotelId &&
                r.RoomNumber == roomNumber &&
                r.DeletedAtUtc == null, ct);

        if (duplicateExists)
            return AdminErrors.Rooms.DuplicateRoomNumber;

        entity.Update(roomNumber, cmd.Floor);
        entity.UpdateStatus(cmd.Status);

        await db.SaveChangesAsync(ct);

        return new RoomDto(
            Id: entity.Id,
            HotelId: entity.HotelId,
            HotelName: entity.Hotel.Name,
            HotelRoomTypeId: entity.HotelRoomTypeId,
            RoomTypeName: entity.HotelRoomType.RoomType.Name,
            RoomNumber: entity.RoomNumber,
            Floor: entity.Floor,
            Status: entity.Status,
            CreatedAtUtc: entity.CreatedAtUtc,
            LastModifiedUtc: entity.LastModifiedUtc);
    }
}