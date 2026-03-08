using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Reviews.Commands.DeleteReview;

public sealed class DeleteReviewCommandHandler(IAppDbContext db)
    : IRequestHandler<DeleteReviewCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(
        DeleteReviewCommand cmd, CancellationToken ct)
    {
        var review = await db.Reviews
            .FirstOrDefaultAsync(r => r.Id == cmd.ReviewId
                && r.DeletedAtUtc == null, ct);

        if (review is null)
            return Error.NotFound("Review.NotFound", "Review not found.");

        if (review.HotelId != cmd.HotelId)
            return Error.NotFound("Review.NotFound", "Review not found.");

        if (!cmd.IsAdmin && review.UserId != cmd.UserId)
            return Error.Forbidden("Review.Forbidden", "You can only delete your own reviews.");

        review.DeletedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result.Deleted;
    }
}
