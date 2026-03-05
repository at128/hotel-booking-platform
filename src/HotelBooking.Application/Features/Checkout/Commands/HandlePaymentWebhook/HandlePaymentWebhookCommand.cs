using HotelBooking.Application.Common.Models.Payment;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Checkout.Commands.HandlePaymentWebhook;

public sealed record HandlePaymentWebhookCommand(
    WebhookParseResult WebhookEvent
) : IRequest<Result<Updated>>;