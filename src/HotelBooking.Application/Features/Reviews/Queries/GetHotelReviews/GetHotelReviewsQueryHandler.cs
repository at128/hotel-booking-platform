using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Reviews;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Hotels;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Reviews.Queries.GetHotelReviews;

public sealed class GetHotelReviewsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetHotelReviewsQuery, Result<HotelReviewsResponse>>
{
    public async Task<Result<HotelReviewsResponse>> Handle(
        GetHotelReviewsQuery query,
        CancellationToken ct)
    {
        var hotelExists = await db.Hotels
            .AsNoTracking()
            .AnyAsync(h => h.Id == query.HotelId, ct);

        if (!hotelExists)
            return HotelErrors.NotFound;

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var reviewsQuery = db.Reviews
            .AsNoTracking()
            .Where(r => r.HotelId == query.HotelId)
            .OrderByDescending(r => r.CreatedAtUtc);

        var totalCount = await reviewsQuery.CountAsync(ct);

        var items = await reviewsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReviewDto(
                r.Id,
                r.HotelId,
                r.BookingId,
                r.UserId,
                null,
                r.Rating,
                r.Title,
                r.Comment,
                r.CreatedAtUtc))
            .ToListAsync(ct);

        return new HotelReviewsResponse(
            Items: items,
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize,
            HasMore: page * pageSize < totalCount);
    }
}