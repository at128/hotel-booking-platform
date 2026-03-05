using HotelBooking.Contracts.Admin;
using HotelBooking.Contracts.Checkout;
using HotelBooking.Contracts.Common;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Checkout.Queries.GetUserBookings;

public sealed record GetUserBookingsQuery(
    Guid UserId,
    string? StatusFilter,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PaginatedResponse<BookingListItemDto>>>;