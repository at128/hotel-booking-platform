using HotelBooking.Contracts.Admin;
using HotelBooking.Contracts.Home;
using HotelBooking.Domain.Common.Results;
using MediatR;
using FeaturedDealDto = HotelBooking.Contracts.Admin.FeaturedDealDto;

namespace HotelBooking.Application.Features.Admin.FeaturedDeals.Commands.CreateFeaturedDeal;

public sealed record CreateFeaturedDealCommand(
    Guid HotelId,
    Guid HotelRoomTypeId,
    decimal OriginalPrice,
    decimal DiscountedPrice,
    int DisplayOrder,
    DateTimeOffset? StartsAtUtc,
    DateTimeOffset? EndsAtUtc
) : IRequest<Result<FeaturedDealDto>>;