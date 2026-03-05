using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Hotels;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Hotels.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Hotels.Commands.AddHotelImage;

public sealed class AddHotelImageCommandHandler(IAppDbContext db)
    : IRequestHandler<AddHotelImageCommand, Result<ImageDto>>
{
    public async Task<Result<ImageDto>> Handle(AddHotelImageCommand cmd, CancellationToken ct)
    {
        var hotel = await db.Hotels
            .FirstOrDefaultAsync(h => h.Id == cmd.HotelId && h.DeletedAtUtc == null, ct);

        if (hotel is null)
            return AdminErrors.Hotels.NotFound;

        const int maxImagesPerHotel = 100;

        var existingImagesCount = await db.Images
            .AsNoTracking()
            .CountAsync(i => i.EntityType == ImageType.Hotel && i.EntityId == cmd.HotelId, ct);

        if (existingImagesCount >= maxImagesPerHotel)
        {
            return Error.Validation(
                "Hotel.ImageLimitExceeded",
                $"This hotel already has the maximum allowed images ({maxImagesPerHotel}).");
        }


        var caption = string.IsNullOrWhiteSpace(cmd.Caption) ? null : cmd.Caption.Trim();

        var sortOrder = cmd.SortOrder ?? await GetNextSortOrderAsync(cmd.HotelId, ct);

        var image = new Image(
            id: Guid.CreateVersion7(),
            entityType: ImageType.Hotel,
            entityId: cmd.HotelId,
            url: cmd.Url,
            caption: caption,
            sortOrder: sortOrder);

        db.Images.Add(image);

        // Optional but useful: set thumbnail automatically if hotel has none
        if (string.IsNullOrWhiteSpace(hotel.ThumbnailUrl))
            hotel.SetThumbnail(cmd.Url);

        await db.SaveChangesAsync(ct);

        return new ImageDto(
            Id: image.Id,
            Url: image.Url,
            Caption: image.Caption,
            SortOrder: image.SortOrder,
            EntityType: image.EntityType.ToString());
    }

    private async Task<int> GetNextSortOrderAsync(Guid hotelId, CancellationToken ct)
    {
        var max = await db.Images
            .AsNoTracking()
            .Where(i => i.EntityType == ImageType.Hotel && i.EntityId == hotelId)
            .Select(i => (int?)i.SortOrder)
            .MaxAsync(ct);

        return (max ?? -1) + 1;
    }
}