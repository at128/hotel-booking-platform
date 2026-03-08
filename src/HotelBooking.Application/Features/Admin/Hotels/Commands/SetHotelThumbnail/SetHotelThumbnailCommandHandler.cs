using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Hotels.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Hotels.Commands.SetHotelThumbnail;

public sealed class SetHotelThumbnailCommandHandler(IAppDbContext db)
    : IRequestHandler<SetHotelThumbnailCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(
        SetHotelThumbnailCommand cmd, CancellationToken ct)
    {
        var hotel = await db.Hotels
            .FirstOrDefaultAsync(h => h.Id == cmd.HotelId && h.DeletedAtUtc == null, ct);

        if (hotel is null)
            return AdminErrors.Hotels.NotFound;

        var image = await db.Images
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == cmd.ImageId
                && i.EntityId == cmd.HotelId
                && i.EntityType == ImageType.Hotel, ct);

        if (image is null)
            return Error.NotFound("Image.NotFound", "Image not found.");

        hotel.SetThumbnail(image.Url);
        await db.SaveChangesAsync(ct);

        return Result.Success;
    }
}