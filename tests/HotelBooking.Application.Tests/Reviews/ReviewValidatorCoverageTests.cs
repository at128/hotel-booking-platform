using FluentValidation.TestHelper;
using HotelBooking.Application.Features.Reviews.Commands.DeleteReview;
using HotelBooking.Application.Features.Reviews.Commands.UpdateReview;
using HotelBooking.Domain.Common.Constants;
using Xunit;

namespace HotelBooking.Application.Tests.Reviews;

public sealed class DeleteReviewCommandValidatorCoverageTests
{
    private readonly DeleteReviewCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var cmd = new DeleteReviewCommand(
            HotelId: Guid.NewGuid(),
            ReviewId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            IsAdmin: false);

        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyIds_HasErrors()
    {
        var cmd = new DeleteReviewCommand(
            HotelId: Guid.Empty,
            ReviewId: Guid.Empty,
            UserId: Guid.Empty,
            IsAdmin: false);

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.HotelId);
        result.ShouldHaveValidationErrorFor(x => x.ReviewId);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}

public sealed class UpdateReviewCommandValidatorCoverageTests
{
    private readonly UpdateReviewCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var cmd = new UpdateReviewCommand(
            HotelId: Guid.NewGuid(),
            ReviewId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            Rating: (short)HotelBookingConstants.Review.MinRating,
            Title: "Great stay",
            Comment: "Everything was good.");

        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_InvalidRatingAndIds_HasErrors()
    {
        var cmd = new UpdateReviewCommand(
            HotelId: Guid.Empty,
            ReviewId: Guid.Empty,
            UserId: Guid.Empty,
            Rating: 0,
            Title: "ok",
            Comment: "ok");

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.HotelId);
        result.ShouldHaveValidationErrorFor(x => x.ReviewId);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
        result.ShouldHaveValidationErrorFor(x => x.Rating);
    }

    [Fact]
    public void Validate_TitleAndCommentTooLong_HasErrors()
    {
        var cmd = new UpdateReviewCommand(
            HotelId: Guid.NewGuid(),
            ReviewId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            Rating: (short)HotelBookingConstants.Review.MaxRating,
            Title: new string('t', HotelBookingConstants.Review.TitleMaxLength + 1),
            Comment: new string('c', HotelBookingConstants.Review.CommentMaxLength + 1));

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Title);
        result.ShouldHaveValidationErrorFor(x => x.Comment);
    }
}
