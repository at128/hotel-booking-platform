using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Hotels.Command.UpdateHotel;

public sealed class UpdateHotelCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateHotelCommand, Result<HotelDto>>
{
    public async Task<Result<HotelDto>> Handle(UpdateHotelCommand cmd, CancellationToken ct)
    {
        var hotel = await db.Hotels
            .Include(h => h.City)
            .Include(h => h.HotelRoomTypes)
            .FirstOrDefaultAsync(h => h.Id == cmd.Id && h.DeletedAtUtc == null, ct);

        if (hotel is null)
            return AdminErrors.Hotels.NotFound;

        // Current domain model does not expose changing CityId via Hotel.Update(...)
        // Keep this explicit to avoid silently ignoring client input.
        if (cmd.CityId != hotel.CityId)
        {
            return Error.Validation(
                "Admin.Hotels.CityChangeNotSupported",
                "Changing hotel city is not supported by this endpoint.");
        }

        var name = cmd.Name.Trim();
        var owner = cmd.Owner.Trim();
        var address = cmd.Address.Trim();
        var description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim();

        // Friendly duplicate check (matches unique index on Name + CityId)
        var exists = await db.Hotels
            .AsNoTracking()
            .AnyAsync(h =>
                h.Id != cmd.Id &&
                h.DeletedAtUtc == null &&
                h.CityId == hotel.CityId &&
                h.Name == name, ct);

        if (exists)
            return AdminErrors.Hotels.AlreadyExists;

        hotel.Update(
            name: name,
            owner: owner,
            address: address,
            starRating: cmd.StarRating,
            description: description,
            latitude: cmd.Latitude,
            longitude: cmd.Longitude,
            checkInTime: hotel.CheckInTime,   // preserve existing values
            checkOutTime: hotel.CheckOutTime);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsHotelNameCityUniqueViolation(ex))
        {
            // Race condition fallback
            return AdminErrors.Hotels.AlreadyExists;
        }

        return new HotelDto(
            hotel.Id,
            hotel.CityId,
            hotel.City.Name,
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
            hotel.HotelRoomTypes.Count(rt => rt.DeletedAtUtc == null),
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