using HotelBooking.Contracts.Reviews;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Reviews.Commands.UpdateReview;

public sealed record UpdateReviewCommand(
    Guid HotelId,
    Guid ReviewId,
    Guid UserId,
    short Rating,
    string? Title,
    string? Comment
) : IRequest<Result<ReviewDto>>;
