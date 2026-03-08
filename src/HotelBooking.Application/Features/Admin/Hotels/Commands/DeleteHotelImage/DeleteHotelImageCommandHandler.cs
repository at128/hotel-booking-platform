using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Hotels.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Hotels.Commands.DeleteHotelImage;

public sealed class DeleteHotelImageCommandHandler(IAppDbContext db)
    : IRequestHandler<DeleteHotelImageCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(
        DeleteHotelImageCommand cmd, CancellationToken ct)
    {
        var image = await db.Images
            .FirstOrDefaultAsync(i => i.Id == cmd.ImageId
                && i.EntityId == cmd.HotelId
                && i.EntityType == ImageType.Hotel, ct);

        if (image is null)
            return Error.NotFound("Image.NotFound", "Image not found.");

        var hotel = await db.Hotels
            .FirstOrDefaultAsync(h => h.Id == cmd.HotelId, ct);

        if (hotel is not null && hotel.ThumbnailUrl == image.Url)
            hotel.SetThumbnail(null);

        db.Images.Remove(image);
        await db.SaveChangesAsync(ct);

        return Result.Deleted;
    }
}