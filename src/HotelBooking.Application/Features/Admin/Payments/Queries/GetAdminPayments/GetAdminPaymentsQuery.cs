using HotelBooking.Contracts.Admin;
using HotelBooking.Contracts.Common;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Payments.Queries.GetAdminPayments;

public sealed record GetAdminPaymentsQuery(
    string? Status,
    string? BookingNumber,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PaginatedResponse<PaymentListItemDto>>>;