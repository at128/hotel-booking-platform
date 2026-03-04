using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Hotels;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Hotels.Command.CreateHotel;

public sealed class CreateHotelCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateHotelCommand, Result<HotelDto>>
{
    public async Task<Result<HotelDto>> Handle(CreateHotelCommand cmd, CancellationToken ct)
    {
        var name = cmd.Name.Trim();
        var owner = cmd.Owner.Trim();
        var address = cmd.Address.Trim();
        var description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim();

        var city = await db.Cities
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cmd.CityId && c.DeletedAtUtc == null, ct);

        if (city is null)
            return AdminErrors.Cities.NotFound;

        var exists = await db.Hotels
            .AsNoTracking()
            .AnyAsync(h =>
                h.DeletedAtUtc == null &&
                h.CityId == cmd.CityId &&
                h.Name == name, ct);

        if (exists)
            return AdminErrors.Hotels.AlreadyExists;

        var hotel = new Hotel(
            id: Guid.CreateVersion7(),
            cityId: cmd.CityId,
            name: name,
            owner: owner,
            address: address,
            starRating: cmd.StarRating,
            description: description,
            latitude: cmd.Latitude,
            longitude: cmd.Longitude);

        db.Hotels.Add(hotel);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsHotelNameCityUniqueViolation(ex))
        {
            // Race condition fallback: another request inserted same hotel in same city
            return AdminErrors.Hotels.AlreadyExists;
        }

        return new HotelDto(
            hotel.Id,
            hotel.CityId,
            city.Name,
            hotel.Name,
            hotel.Owner,
            hotel.Address,
            hotel.StarRating,
            hotel.Description,
            hotel.Latitude,
            hotel.Longitude,
            hotel.MinPricePerNight,
            hotel.AverageRating,
            hotel.ReviewCount,
            0, // no room types yet at creation time
            hotel.CreatedAtUtc,
            hotel.LastModifiedUtc);
    }

    private static bool IsHotelNameCityUniqueViolation(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;

        return msg.Contains("IX_hotels_Name_CityId", StringComparison.OrdinalIgnoreCase) ||
               (msg.Contains("hotels", StringComparison.OrdinalIgnoreCase) &&
                msg.Contains("unique", StringComparison.OrdinalIgnoreCase));
    }
}