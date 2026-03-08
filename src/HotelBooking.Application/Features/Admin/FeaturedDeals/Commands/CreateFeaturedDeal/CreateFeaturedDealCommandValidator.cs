using FluentValidation;
using FeaturedDealDto = HotelBooking.Contracts.Admin.FeaturedDealDto;

namespace HotelBooking.Application.Features.Admin.FeaturedDeals.Commands.CreateFeaturedDeal;

public sealed class CreateFeaturedDealCommandValidator : AbstractValidator<CreateFeaturedDealCommand>
{
    public CreateFeaturedDealCommandValidator()
    {
        RuleFor(x => x.HotelId).NotEmpty();
        RuleFor(x => x.HotelRoomTypeId).NotEmpty();
        RuleFor(x => x.OriginalPrice).GreaterThan(0);
        RuleFor(x => x.DiscountedPrice).GreaterThan(0)
            .LessThan(x => x.OriginalPrice)
            .WithMessage("Discounted price must be less than original price.");
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EndsAtUtc)
            .GreaterThan(x => x.StartsAtUtc)
            .When(x => x.StartsAtUtc.HasValue && x.EndsAtUtc.HasValue)
            .WithMessage("End date must be after start date.");
    }
}