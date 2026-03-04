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
        var entity = await db.HotelRoomTypes
            .Include(x => x.Hotel)
            .Include(x => x.RoomType)
            .Include(x => x.Rooms)
            .FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);

        if (entity is null)
            return AdminErrors.Rooms.NotFound(cmd.Id);

        entity.Update(
            pricePerNight: cmd.PricePerNight,
            adultCapacity: cmd.AdultCapacity,
            childCapacity: cmd.ChildCapacity,
            description: string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim());

        await db.SaveChangesAsync(ct);

        return new RoomDto(
            entity.Id,
            entity.HotelId,
            entity.Hotel.Name,
            entity.RoomTypeId,
            entity.RoomType.Name,
            entity.PricePerNight,
            entity.AdultCapacity,
            entity.ChildCapacity,
            entity.MaxOccupancy,
            entity.Description,
            entity.Rooms.Count(r => r.DeletedAtUtc == null),
            entity.CreatedAtUtc,
            entity.LastModifiedUtc);
    }
}