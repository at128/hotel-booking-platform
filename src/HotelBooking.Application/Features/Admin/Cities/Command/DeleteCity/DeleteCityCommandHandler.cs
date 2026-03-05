using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Cities.Command.DeleteCity;

public sealed class DeleteCityCommandHandler(IAppDbContext db)
    : IRequestHandler<DeleteCityCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteCityCommand cmd, CancellationToken ct)
    {
        var city = await db.Cities
            .Include(c => c.Hotels)
            .FirstOrDefaultAsync(c => c.Id == cmd.Id && c.DeletedAtUtc == null, ct);

        if (city is null)
            return AdminErrors.Cities.NotFound; 

        var activeHotels = city.Hotels.Count(h => h.DeletedAtUtc == null);
        if (activeHotels > 0)
            return AdminErrors.Cities.HasRelatedHotels; 

        city.DeletedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result.Deleted;
    }
}