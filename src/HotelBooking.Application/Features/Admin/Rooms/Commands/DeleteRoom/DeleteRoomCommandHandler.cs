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
        var entity = await db.HotelRoomTypes
            .FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);

        if (entity is null)
            return AdminErrors.Rooms.NotFound(cmd.Id);

        var hasConfirmedBookings = await db.BookingRooms
            .AsNoTracking()
            .AnyAsync(br =>
                br.HotelRoomTypeId == cmd.Id &&
                br.Booking.Status == BookingStatus.Confirmed, ct);

        if (hasConfirmedBookings)
            return AdminErrors.Rooms.HasActiveBookings;

        entity.DeletedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result.Deleted;
    }
}