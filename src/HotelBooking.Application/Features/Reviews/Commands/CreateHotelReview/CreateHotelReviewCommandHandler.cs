using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Reviews;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Reviews;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Reviews.Commands.CreateHotelReview;

public sealed class CreateHotelReviewCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateHotelReviewCommand, Result<ReviewDto>>
{
    public async Task<Result<ReviewDto>> Handle(CreateHotelReviewCommand cmd, CancellationToken ct)
    {
        // 1) booking must exist and belong to the user
        var booking = await db.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b =>
                b.Id == cmd.BookingId &&
                b.UserId == cmd.UserId 
                , ct);

        if (booking is null)
        {
            return Error.NotFound(
                code: "Reviews.BookingNotFound",
                description: "Booking was not found for this user.");
        }

        if (booking.HotelId != cmd.HotelId)
        {
            return Error.Validation(
                code: "Reviews.BookingHotelMismatch",
                description: "Booking does not belong to the specified hotel.");
        }

        if (booking.Status is BookingStatus.Pending or BookingStatus.Failed or BookingStatus.Cancelled)
        {
            return Error.Validation(
                code: "Reviews.BookingNotEligible",
                description: "Only completed/eligible bookings can be reviewed.");
        }

        var alreadyReviewed = await db.Reviews
            .AsNoTracking()
            .AnyAsync(r => r.BookingId == cmd.BookingId && r.DeletedAtUtc == null, ct);

        if (alreadyReviewed)
        {
            return Error.Conflict(
                code: "Reviews.DuplicateReview",
                description: "A review for this booking already exists.");
        }

        var title = string.IsNullOrWhiteSpace(cmd.Title) ? null : cmd.Title.Trim();
        var comment = string.IsNullOrWhiteSpace(cmd.Comment) ? null : cmd.Comment.Trim();

        var review = new Review(
            id: Guid.CreateVersion7(),
            userId: cmd.UserId,
            hotelId: cmd.HotelId,
            bookingId: cmd.BookingId,
            rating: cmd.Rating,
            title: title,
            comment: comment);

        db.Reviews.Add(review);

        await db.SaveChangesAsync(ct);

        await RecalculateHotelReviewSummaryAsync(cmd.HotelId, ct);

        return new ReviewDto(
            Id: review.Id,
            HotelId: review.HotelId,
            BookingId: review.BookingId,
            UserId: review.UserId,
            Rating: review.Rating,
            Title: review.Title,
            Comment: review.Comment,
            CreatedAtUtc: review.CreatedAtUtc);
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

        if (aggregate is null)
            return;

        var hotel = await db.Hotels.FirstOrDefaultAsync(h => h.Id == hotelId && h.DeletedAtUtc == null, ct);
        if (hotel is null)
            return;

        hotel.UpdateReviewSummary(aggregate.Avg, aggregate.Count);
        await db.SaveChangesAsync(ct);
    }
}