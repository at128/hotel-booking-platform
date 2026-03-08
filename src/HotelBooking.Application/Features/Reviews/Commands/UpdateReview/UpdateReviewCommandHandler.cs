using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Reviews;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Reviews.Commands.UpdateReview;

public sealed class UpdateReviewCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateReviewCommand, Result<ReviewDto>>
{
    public async Task<Result<ReviewDto>> Handle(
        UpdateReviewCommand cmd, CancellationToken ct)
    {
        var review = await db.Reviews
            .FirstOrDefaultAsync(r => r.Id == cmd.ReviewId
                && r.DeletedAtUtc == null, ct);

        if (review is null)
            return Error.NotFound("Review.NotFound", "Review not found.");

        if (review.HotelId != cmd.HotelId)
            return Error.NotFound("Review.NotFound", "Review not found.");

        if (review.UserId != cmd.UserId)
            return Error.Forbidden("Review.Forbidden", "You can only edit your own reviews.");

        review.Update(cmd.Rating, cmd.Title, cmd.Comment);
        await db.SaveChangesAsync(ct);
        await RecalculateHotelReviewSummaryAsync(review.HotelId, ct);

        return new ReviewDto(
            review.Id,
            review.HotelId,
            review.BookingId,
            review.UserId,
            null,
            review.Rating,
            review.Title,
            review.Comment,
            review.CreatedAtUtc);
    }

    private async Task RecalculateHotelReviewSummaryAsync(Guid hotelId, CancellationToken ct)
    {
        var aggregate = await db.Reviews
            .AsNoTracking()
            .Where(r => r.HotelId == hotelId && r.DeletedAtUtc == null)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Avg = g.Average(x => (double)x.Rating),
                Count = g.Count()
            })
            .FirstOrDefaultAsync(ct);

        var hotel = await db.Hotels
            .FirstOrDefaultAsync(h => h.Id == hotelId && h.DeletedAtUtc == null, ct);

        if (hotel is null)
            return;

        if (aggregate is null)
            hotel.UpdateReviewSummary(0, 0);
        else
            hotel.UpdateReviewSummary(aggregate.Avg, aggregate.Count);

        await db.SaveChangesAsync(ct);
    }
}
