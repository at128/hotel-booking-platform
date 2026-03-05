using FluentValidation;
using HotelBooking.Domain.Common.Constants;

namespace HotelBooking.Application.Features.Reviews.Commands.CreateHotelReview;

public sealed class CreateHotelReviewCommandValidator : AbstractValidator<CreateHotelReviewCommand>
{
    public CreateHotelReviewCommandValidator()
    {
        RuleFor(x => x.HotelId).NotEmpty();
        RuleFor(x => x.BookingId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.Rating)
            .InclusiveBetween(
                (short)HotelBookingConstants.Review.MinRating,
                (short)HotelBookingConstants.Review.MaxRating);

        RuleFor(x => x.Title)
            .MaximumLength(HotelBookingConstants.Review.TitleMaxLength)
            .When(x => x.Title is not null);

        RuleFor(x => x.Comment)
            .MaximumLength(HotelBookingConstants.Review.CommentMaxLength)
            .When(x => x.Comment is not null);
    }
}