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
}
