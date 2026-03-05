using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Checkout.Commands.ExpirePendingPayments;

public sealed record ExpirePendingPaymentsCommand(int BatchSize) : IRequest<Result<Updated>>;