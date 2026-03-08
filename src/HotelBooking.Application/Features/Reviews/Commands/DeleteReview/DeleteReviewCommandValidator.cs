using FluentValidation;

namespace HotelBooking.Application.Features.Reviews.Commands.DeleteReview;

public sealed class DeleteReviewCommandValidator : AbstractValidator<DeleteReviewCommand>
{
    public DeleteReviewCommandValidator()
    {
        RuleFor(x => x.HotelId).NotEmpty();
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
