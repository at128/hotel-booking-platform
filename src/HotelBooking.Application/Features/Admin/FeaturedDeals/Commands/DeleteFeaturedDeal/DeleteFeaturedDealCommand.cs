using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.FeaturedDeals.Commands.DeleteFeaturedDeal;

public sealed record DeleteFeaturedDealCommand(Guid Id) : IRequest<Result<Deleted>>;