using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Contracts.Home;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Hotels;
using MediatR;
using Microsoft.EntityFrameworkCore;
using FeaturedDealDto = HotelBooking.Contracts.Admin.FeaturedDealDto;

namespace HotelBooking.Application.Features.Admin.FeaturedDeals.Commands.CreateFeaturedDeal;

public sealed class CreateFeaturedDealCommandHandler(
    IAppDbContext db,
    ICacheInvalidator? cacheInvalidator = null)
    : IRequestHandler<CreateFeaturedDealCommand, Result<FeaturedDealDto>>
{
    private const string FeaturedDealsCacheKey = "home:featured-deals";

    public async Task<Result<FeaturedDealDto>> Handle(
        CreateFeaturedDealCommand cmd, CancellationToken ct)
    {
        var hotel = await db.Hotels
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == cmd.HotelId && h.DeletedAtUtc == null, ct);

        if (hotel is null)
            return AdminErrors.Hotels.NotFound;

        var hrt = await db.HotelRoomTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == cmd.HotelRoomTypeId
                && r.HotelId == cmd.HotelId
                && r.DeletedAtUtc == null, ct);

        if (hrt is null)
            return AdminErrors.HotelRoomTypes.NotFound(hotel.Id);

        var duplicate = await db.FeaturedDeals
            .AnyAsync(d => d.HotelRoomTypeId == cmd.HotelRoomTypeId, ct);

        if (duplicate)
            return Error.Conflict("FeaturedDeal.Duplicate",
                "A featured deal already exists for this room type.");

        var deal = new FeaturedDeal(
            id: Guid.CreateVersion7(),
            hotelId: cmd.HotelId,
            hotelRoomTypeId: cmd.HotelRoomTypeId,
            originalPrice: cmd.OriginalPrice,
            discountedPrice: cmd.DiscountedPrice,
            displayOrder: cmd.DisplayOrder,
            startsAtUtc: cmd.StartsAtUtc,
            endsAtUtc: cmd.EndsAtUtc);

        db.FeaturedDeals.Add(deal);
        await db.SaveChangesAsync(ct);
        if (cacheInvalidator is not null)
            await cacheInvalidator.RemoveAsync(FeaturedDealsCacheKey, ct);

        return new FeaturedDealDto(
            deal.Id, deal.HotelId, hotel.Name,
            deal.HotelRoomTypeId, deal.OriginalPrice,
            deal.DiscountedPrice, deal.DisplayOrder,
            deal.StartsAtUtc, deal.EndsAtUtc,
            deal.IsActive());
    }
}
