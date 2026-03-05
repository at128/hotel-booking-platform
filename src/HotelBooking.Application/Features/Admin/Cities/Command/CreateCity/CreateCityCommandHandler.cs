using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Hotels;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Cities.Command.CreateCity;

public sealed class CreateCityCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateCityCommand, Result<CityDto>>
{
    public async Task<Result<CityDto>> Handle(
        CreateCityCommand cmd, CancellationToken ct)
    {
        var name = cmd.Name.Trim();
        var country = cmd.Country.Trim();
        var postOffice = string.IsNullOrWhiteSpace(cmd.PostOffice)
            ? null
            : cmd.PostOffice.Trim();

        var exists = await db.Cities
            .AsNoTracking()
            .AnyAsync(c =>
                c.DeletedAtUtc == null &&
                c.Name == name &&
                c.Country == country, ct);

        if (exists)
            return AdminErrors.Cities.AlreadyExists; 

        var city = new City(
            Guid.CreateVersion7(),
            name,
            country,
            postOffice);

        db.Cities.Add(city);

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
            0,
            city.CreatedAtUtc,
            null);
    }

    private static bool IsCityNameCountryUniqueViolation(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;

        return msg.Contains("IX_cities_Name_Country", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("cities", StringComparison.OrdinalIgnoreCase) &&
               msg.Contains("unique", StringComparison.OrdinalIgnoreCase);
    }
}