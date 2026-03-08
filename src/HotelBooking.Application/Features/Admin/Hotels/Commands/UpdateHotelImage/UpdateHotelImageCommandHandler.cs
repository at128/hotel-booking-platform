using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Contracts.Hotels;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Hotels.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Hotels.Commands.UpdateHotelImage;

public sealed class UpdateHotelImageCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateHotelImageCommand, Result<ImageDto>>
{
    public async Task<Result<ImageDto>> Handle(
        UpdateHotelImageCommand cmd, CancellationToken ct)
    {
        var image = await db.Images
            .FirstOrDefaultAsync(i => i.Id == cmd.ImageId
                && i.EntityId == cmd.HotelId
                && i.EntityType == ImageType.Hotel, ct);

        if (image is null)
            return Error.NotFound("Image.NotFound", "Image not found.");

        image.Update(cmd.Caption, cmd.SortOrder);
        await db.SaveChangesAsync(ct);

        return new ImageDto(image.Id, image.Url, image.Caption, image.SortOrder);
    }
}