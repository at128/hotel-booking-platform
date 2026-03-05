using HotelBooking.Contracts.Checkout;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Checkout.Commands.CancelBooking;

public sealed record CancelBookingCommand(
    Guid BookingId,
    Guid RequestingUserId,
    bool IsAdmin,
    string? Reason
) : IRequest<Result<CancellationDetailsResponse>>;