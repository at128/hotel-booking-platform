using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.FeaturedDeals.Commands.DeleteFeaturedDeal;

public sealed class DeleteFeaturedDealCommandHandler(
    IAppDbContext db,
    ICacheInvalidator? cacheInvalidator = null)
    : IRequestHandler<DeleteFeaturedDealCommand, Result<Deleted>>
{
    private const string FeaturedDealsCacheKey = "home:featured-deals";

    public async Task<Result<Deleted>> Handle(
        DeleteFeaturedDealCommand cmd, CancellationToken ct)
    {
        var deal = await db.FeaturedDeals
            .FirstOrDefaultAsync(d => d.Id == cmd.Id, ct);

        if (deal is null)
            return Error.NotFound("FeaturedDeal.NotFound", "Featured deal not found.");

        db.FeaturedDeals.Remove(deal);
        await db.SaveChangesAsync(ct);
        if (cacheInvalidator is not null)
            await cacheInvalidator.RemoveAsync(FeaturedDealsCacheKey, ct);

        return Result.Deleted;
    }
}
