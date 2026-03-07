using HotelBooking.Contracts.Reviews;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Reviews.Queries.GetHotelReviews;

public sealed record GetHotelReviewsQuery(
    Guid HotelId,
    int Page = 1,
    int PageSize = 10)
    : IRequest<Result<HotelReviewsResponse>>;