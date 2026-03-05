using HotelBooking.Contracts.Checkout;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Checkout.Queries.GetBooking;

public sealed record GetBookingQuery(
    Guid BookingId,
    Guid RequestingUserId,
    bool IsAdmin
) : IRequest<Result<BookingDetailsResponse>>;