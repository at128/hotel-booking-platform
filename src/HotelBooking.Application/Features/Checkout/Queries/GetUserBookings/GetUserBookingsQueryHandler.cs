using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Common;
using HotelBooking.Contracts.Checkout;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Checkout.Queries.GetUserBookings;

public sealed class GetUserBookingsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetUserBookingsQuery, Result<PaginatedResponse<BookingListItemDto>>>
{
    public async Task<Result<PaginatedResponse<BookingListItemDto>>> Handle(
        GetUserBookingsQuery q,
        CancellationToken ct)
    {
        var pageSize = Math.Clamp(q.PageSize, 1, 50);
        var page = Math.Max(1, q.Page);

        var query = db.Bookings
            .AsNoTracking()
            .Where(b => b.UserId == q.UserId);

        if (!string.IsNullOrWhiteSpace(q.StatusFilter) &&
            Enum.TryParse<BookingStatus>(q.StatusFilter, ignoreCase: true, out var statusEnum))
        {
            query = query.Where(b => b.Status == statusEnum);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(b => b.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BookingListItemDto(
                b.Id,
                b.BookingNumber,
                b.HotelName,
                b.CheckIn,
                b.CheckOut,
                b.TotalAmount,
                b.Status.ToString(),
                b.Payments
                    .OrderByDescending(p => p.CreatedAtUtc)
                    .Select(p => p.Status.ToString())
                    .FirstOrDefault()))
            .ToListAsync(ct);

        return new PaginatedResponse<BookingListItemDto>(
            Items: items,
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize,
            HasMore: (page * pageSize) < totalCount);
    }
}