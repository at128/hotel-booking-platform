using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Cities.Command.UpdateCity;

public sealed class UpdateCityCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateCityCommand, Result<CityDto>>
{
    public async Task<Result<CityDto>> Handle(UpdateCityCommand cmd, CancellationToken ct)
    {
        var city = await db.Cities
            .FirstOrDefaultAsync(c => c.Id == cmd.Id && c.DeletedAtUtc == null, ct);

        if (city is null)
            return AdminErrors.Cities.NotFound; 

        var name = cmd.Name.Trim();
        var country = cmd.Country.Trim();
        var postOffice = string.IsNullOrWhiteSpace(cmd.PostOffice)
            ? null
            : cmd.PostOffice.Trim();

        var exists = await db.Cities
            .AsNoTracking()
            .AnyAsync(c =>
                c.Id != cmd.Id &&
                c.DeletedAtUtc == null &&
                c.Name == name &&
                c.Country == country, ct);

        if (exists)
            return AdminErrors.Cities.AlreadyExists;

        city.Update(name, country, postOffice);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsCityNameCountryUniqueViolation(ex))
        {
            return AdminErrors.Cities.AlreadyExists; 
        }

        return new CityDto(
            city.Id,
            city.Name,
            city.Country,
            city.PostOffice,
            city.Hotels.Count(h => h.DeletedAtUtc == null),
            city.CreatedAtUtc,
            city.LastModifiedUtc);
    }

    private static bool IsCityNameCountryUniqueViolation(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;

        return msg.Contains("IX_cities_Name_Country", StringComparison.OrdinalIgnoreCase) ||
               (msg.Contains("cities", StringComparison.OrdinalIgnoreCase) &&
                msg.Contains("unique", StringComparison.OrdinalIgnoreCase));
    }
}