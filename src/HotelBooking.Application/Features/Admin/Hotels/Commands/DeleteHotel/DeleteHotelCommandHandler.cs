using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Hotels.Command.DeleteHotel;

public sealed class DeleteHotelCommandHandler(IAppDbContext db)
    : IRequestHandler<DeleteHotelCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteHotelCommand cmd, CancellationToken ct)
    {
        var hotel = await db.Hotels
            .FirstOrDefaultAsync(h => h.Id == cmd.Id && h.DeletedAtUtc == null, ct);

        if (hotel is null)
            return AdminErrors.Hotels.NotFound;

        // Guard: do not allow deleting a hotel that has bookings
        var hasBookings = await db.Bookings
            .AsNoTracking()
            .AnyAsync(b => b.HotelId == cmd.Id, ct);

        if (hasBookings)
            return AdminErrors.Hotels.HasActiveBookings;

        hotel.DeletedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result.Deleted;
    }
}