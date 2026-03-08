using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Hotels.Commands.UnlinkService;

public sealed class UnlinkServiceFromHotelCommandHandler(IAppDbContext db)
    : IRequestHandler<UnlinkServiceFromHotelCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(
        UnlinkServiceFromHotelCommand cmd, CancellationToken ct)
    {
        var hs = await db.HotelServices
            .FirstOrDefaultAsync(x => x.HotelId == cmd.HotelId
                && x.ServiceId == cmd.ServiceId, ct);

        if (hs is null)
            return Error.NotFound("HotelService.NotFound",
                "Service link not found for this hotel.");

        db.HotelServices.Remove(hs);
        await db.SaveChangesAsync(ct);

        return Result.Deleted;
    }
}