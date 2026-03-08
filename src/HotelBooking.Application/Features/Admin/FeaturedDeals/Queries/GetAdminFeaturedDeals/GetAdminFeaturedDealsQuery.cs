using HotelBooking.Contracts.Admin;
using HotelBooking.Contracts.Common;
using HotelBooking.Contracts.Home;
using HotelBooking.Domain.Common.Results;
using MediatR;
using FeaturedDealDto = HotelBooking.Contracts.Admin.FeaturedDealDto;

namespace HotelBooking.Application.Features.Admin.FeaturedDeals.Queries.GetAdminFeaturedDeals;

public sealed record GetAdminFeaturedDealsQuery(
    string? Search,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PaginatedResponse<FeaturedDealDto>>>;