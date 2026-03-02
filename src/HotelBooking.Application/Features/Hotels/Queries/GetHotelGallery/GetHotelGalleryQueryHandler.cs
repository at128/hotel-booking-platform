using HotelBooking.Contracts.Hotels;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Hotels.Enums;
using HotelBooking.Domain.Hotels;
using MediatR;
using Microsoft.EntityFrameworkCore;
using HotelBooking.Application.Common.Interfaces;

namespace HotelBooking.Application.Features.Hotels.Queries.GetHotelGallery;

public sealed class GetHotelGalleryQueryHandler(IAppDbContext context)
    : IRequestHandler<GetHotelGalleryQuery, Result<HotelGalleryResponse>>
{
    public async Task<Result<HotelGalleryResponse>> Handle(
        GetHotelGalleryQuery query, CancellationToken ct)
    {
        var hotelExists = await context.Hotels
            .AnyAsync(h => h.Id == query.HotelId, ct);

        if (!hotelExists)
            return HotelErrors.NotFound;

        var images = await context.Images
            .Where(i => i.EntityType == ImageType.Hotel && i.EntityId == query.HotelId)
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.CreatedAtUtc)
            .Select(i => new ImageDto(
                i.Id,
                i.Url,
                i.Caption,
                i.SortOrder,
                i.EntityType.ToString()))
            .ToListAsync(ct);

        return new HotelGalleryResponse(query.HotelId, images);
    }
}