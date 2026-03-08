using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Hotels;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Hotels.Commands.LinkService;

public sealed class LinkServiceToHotelCommandHandler(IAppDbContext db)
    : IRequestHandler<LinkServiceToHotelCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(
        LinkServiceToHotelCommand cmd, CancellationToken ct)
    {
        var hotel = await db.Hotels
            .AsNoTracking()
            .AnyAsync(h => h.Id == cmd.HotelId && h.DeletedAtUtc == null, ct);

        if (!hotel)
            return AdminErrors.Hotels.NotFound;

        var service = await db.Services
            .AsNoTracking()
            .AnyAsync(s => s.Id == cmd.ServiceId && s.DeletedAtUtc == null, ct);

        if (!service)
            return Error.NotFound("Service.NotFound", "Service not found.");

        var exists = await db.HotelServices
            .AnyAsync(hs => hs.HotelId == cmd.HotelId && hs.ServiceId == cmd.ServiceId, ct);

        if (exists)
            return Error.Conflict("HotelService.Duplicate",
                "This service is already linked to the hotel.");

        var hotelService = new HotelService(
            id: Guid.CreateVersion7(),
            hotelId: cmd.HotelId,
            serviceId: cmd.ServiceId,
            price: cmd.Price,
            isFree: cmd.IsFree);

        db.HotelServices.Add(hotelService);
        await db.SaveChangesAsync(ct);

        return Result.Success;
    }
}