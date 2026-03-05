using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Rooms;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Commands.CreateRoomType;

public sealed class CreateRoomTypeCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateRoomTypeCommand, Result<RoomTypeDto>>
{
    public async Task<Result<RoomTypeDto>> Handle(CreateRoomTypeCommand cmd, CancellationToken ct)
    {
        var name = cmd.Name.Trim();
        var description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim();

        var exists = await db.RoomTypes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(rt => rt.Name == name, ct);

        if (exists)
            return AdminErrors.RoomTypes.AlreadyExists;

        var entity = new RoomType(
            id: Guid.CreateVersion7(),
            name: name,
            description: description);

        db.RoomTypes.Add(entity);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsRoomTypeNameUniqueViolation(ex))
        {
            return AdminErrors.RoomTypes.AlreadyExists;
        }

        return new RoomTypeDto(
            entity.Id,
            entity.Name,
            entity.Description,
            0,
            entity.CreatedAtUtc,
            entity.LastModifiedUtc);
    }

    private static bool IsRoomTypeNameUniqueViolation(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;

        return msg.Contains("IX_room_types_Name", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("room_types", StringComparison.OrdinalIgnoreCase) &&
                msg.Contains("unique", StringComparison.OrdinalIgnoreCase);
    }
}