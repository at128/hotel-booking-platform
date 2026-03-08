using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.HotelRoomTypes.Commands.UpdateHotelRoomType;

public sealed class UpdateHotelRoomTypeCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateHotelRoomTypeCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(UpdateHotelRoomTypeCommand cmd, CancellationToken ct)
    {
        var entity = await db.HotelRoomTypes
            .FirstOrDefaultAsync(x => x.Id == cmd.Id && x.DeletedAtUtc == null, ct);

        if (entity is null)
            return AdminErrors.HotelRoomTypes.NotFound(cmd.Id);

        entity.UpdatePricing(cmd.PricePerNight);
        entity.UpdateOccupancy(cmd.AdultCapacity, cmd.ChildCapacity, cmd.MaxOccupancy);
        entity.UpdateDescription(cmd.Description);

        await db.SaveChangesAsync(ct);
        return Result.Updated;
    }
}