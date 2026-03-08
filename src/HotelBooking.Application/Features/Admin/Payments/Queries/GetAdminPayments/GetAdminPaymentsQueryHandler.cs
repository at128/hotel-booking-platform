using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Contracts.Common;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Payments.Queries.GetAdminPayments;

public sealed class GetAdminPaymentsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetAdminPaymentsQuery, Result<PaginatedResponse<PaymentListItemDto>>>
{
    public async Task<Result<PaginatedResponse<PaymentListItemDto>>> Handle(
        GetAdminPaymentsQuery query, CancellationToken ct)
    {
        var q = db.Payments
            .Include(p => p.Booking)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status)
            && Enum.TryParse<PaymentStatus>(query.Status, true, out var status))
        {
            q = q.Where(p => p.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.BookingNumber))
        {
            q = q.Where(p => p.Booking.BookingNumber.Contains(query.BookingNumber));
        }

        q = q.OrderByDescending(p => p.CreatedAtUtc);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new PaymentListItemDto(
                p.Id,
                p.BookingId,
                p.Booking.BookingNumber,
                p.Amount,
                p.Method.ToString(),
                p.Status.ToString(),
                p.TransactionRef,
                p.CreatedAtUtc,
                p.PaidAtUtc))
            .ToListAsync(ct);

        var hasMore = query.Page * query.PageSize < total;

        return new PaginatedResponse<PaymentListItemDto>(
            items,
            total,
            query.Page,
            query.PageSize,
            hasMore);
    }
}