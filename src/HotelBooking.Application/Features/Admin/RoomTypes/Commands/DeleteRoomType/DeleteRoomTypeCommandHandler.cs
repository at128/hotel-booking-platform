using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Commands.DeleteRoomType;

public sealed class DeleteRoomTypeCommandHandler(IAppDbContext db)
    : IRequestHandler<DeleteRoomTypeCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteRoomTypeCommand cmd, CancellationToken ct)
    {
        var entity = await db.RoomTypes
            .FirstOrDefaultAsync(rt => rt.Id == cmd.Id && rt.DeletedAtUtc == null, ct);

        if (entity is null)
            return AdminErrors.RoomTypes.NotFound(cmd.Id);

        var hasAssignments = await db.HotelRoomTypes
            .AsNoTracking()
            .AnyAsync(x => x.RoomTypeId == cmd.Id && x.DeletedAtUtc == null, ct);

        if (hasAssignments)
            return AdminErrors.RoomTypes.HasRelatedHotelAssignments;

        entity.DeletedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result.Deleted;
    }
}