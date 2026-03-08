using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Rooms.Commands.DeleteRoom;

public sealed class DeleteRoomCommandHandler(IAppDbContext db)
    : IRequestHandler<DeleteRoomCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteRoomCommand cmd, CancellationToken ct)
    {
        var entity = await db.Rooms
            .FirstOrDefaultAsync(x => x.Id == cmd.Id && x.DeletedAtUtc == null, ct);

        if (entity is null)
            return AdminErrors.Rooms.NotFound(cmd.Id);


        var hasActiveBookings = await db.BookingRooms
            .AsNoTracking()
            .AnyAsync(br => br.RoomId == cmd.Id
                && (br.Booking.Status == BookingStatus.Pending
                    || br.Booking.Status == BookingStatus.Confirmed
                    || br.Booking.Status == BookingStatus.CheckedIn), ct);

        if (hasActiveBookings)
            return Error.Conflict("Room.HasActiveBookings",
                "Cannot delete room with active bookings.");

        entity.DeletedAtUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        return Result.Deleted;
    }
}