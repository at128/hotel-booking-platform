using HotelBooking.Contracts.Checkout;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Checkout.Commands.CreateCheckoutHold;

public sealed record CreateCheckoutHoldCommand(
    Guid UserId,
    string? Notes) : IRequest<Result<CheckoutHoldResponse>>;