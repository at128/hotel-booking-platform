using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Contracts.Home;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using FeaturedDealDto = HotelBooking.Contracts.Admin.FeaturedDealDto;

namespace HotelBooking.Application.Features.Admin.FeaturedDeals.Commands.UpdateFeaturedDeal;

public sealed class UpdateFeaturedDealCommandHandler(
    IAppDbContext db,
    ICacheInvalidator? cacheInvalidator = null)
    : IRequestHandler<UpdateFeaturedDealCommand, Result<FeaturedDealDto>>
{
    private const string FeaturedDealsCacheKey = "home:featured-deals";

    public async Task<Result<FeaturedDealDto>> Handle(
        UpdateFeaturedDealCommand cmd, CancellationToken ct)
    {
        var deal = await db.FeaturedDeals
            .Include(d => d.Hotel)
            .FirstOrDefaultAsync(d => d.Id == cmd.Id, ct);

        if (deal is null)
            return Error.NotFound("FeaturedDeal.NotFound", "Featured deal not found.");

        deal.Update(cmd.OriginalPrice, cmd.DiscountedPrice,
            cmd.DisplayOrder, cmd.StartsAtUtc, cmd.EndsAtUtc);

        await db.SaveChangesAsync(ct);
        if (cacheInvalidator is not null)
            await cacheInvalidator.RemoveAsync(FeaturedDealsCacheKey, ct);

        return new FeaturedDealDto(
            deal.Id, deal.HotelId, deal.Hotel.Name,
            deal.HotelRoomTypeId, deal.OriginalPrice,
            deal.DiscountedPrice, deal.DisplayOrder,
            deal.StartsAtUtc, deal.EndsAtUtc,
            deal.IsActive());
    }
}
