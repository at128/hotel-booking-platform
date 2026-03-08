using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.HotelRoomTypes.Commands.DeleteHotelRoomType;

public sealed class DeleteHotelRoomTypeCommandHandler(IAppDbContext db)
    : IRequestHandler<DeleteHotelRoomTypeCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteHotelRoomTypeCommand cmd, CancellationToken ct)
    {
        var entity = await db.HotelRoomTypes
            .FirstOrDefaultAsync(x => x.Id == cmd.Id && x.DeletedAtUtc == null, ct);

        if (entity is null)
            return AdminErrors.HotelRoomTypes.NotFound(cmd.Id);

        var hasRooms = await db.Rooms
                .AnyAsync(r => r.HotelRoomTypeId == cmd.Id && r.DeletedAtUtc == null, ct);

        if (hasRooms)
            return AdminErrors.HotelRoomTypes.HasAssignedRooms;

        var hasPendingBookings = await db.BookingRooms
            .AnyAsync(br =>
                br.HotelRoomTypeId == cmd.Id &&
                br.Booking.Status != BookingStatus.Cancelled &&
                br.Booking.Status != BookingStatus.Failed, ct);

        if (hasPendingBookings)
            return AdminErrors.HotelRoomTypes.HasPendingBookings;

        var hasActiveHolds = await db.CheckoutHolds
            .AnyAsync(ch =>
                ch.HotelRoomTypeId == cmd.Id &&
                !ch.IsReleased &&
                ch.ExpiresAtUtc > DateTimeOffset.UtcNow, ct);

        if (hasActiveHolds)
            return AdminErrors.HotelRoomTypes.HasActiveHolds;

        entity.DeletedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result.Deleted;
    }
}