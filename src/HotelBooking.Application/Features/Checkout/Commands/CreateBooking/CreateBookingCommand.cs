using HotelBooking.Contracts.Checkout;
using HotelBooking.Domain.Common.Results;
using MediatR;

public sealed record CreateBookingCommand(
    Guid UserId,
    string UserEmail,
    List<Guid> HoldIds,
    string? Notes
) : IRequest<Result<CreateBookingResponse>>;