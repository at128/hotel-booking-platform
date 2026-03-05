using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Commands.UpdateRoomType;

public sealed class UpdateRoomTypeCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateRoomTypeCommand, Result<RoomTypeDto>>
{
    public async Task<Result<RoomTypeDto>> Handle(UpdateRoomTypeCommand cmd, CancellationToken ct)
    {
        var entity = await db.RoomTypes
            .Include(rt => rt.HotelRoomTypes)
            .FirstOrDefaultAsync(rt => rt.Id == cmd.Id && rt.DeletedAtUtc == null, ct);

        if (entity is null)
            return AdminErrors.RoomTypes.NotFound(cmd.Id);

        var name = cmd.Name.Trim();
        var description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim();

        var exists = await db.RoomTypes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(rt => rt.Id != cmd.Id && rt.Name == name, ct);

        if (exists)
            return AdminErrors.RoomTypes.AlreadyExists;

        entity.Update(name, description);

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
            entity.HotelRoomTypes.Count(x => x.DeletedAtUtc == null),
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