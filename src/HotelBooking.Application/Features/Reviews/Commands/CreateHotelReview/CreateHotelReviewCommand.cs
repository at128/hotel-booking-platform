using HotelBooking.Contracts.Reviews;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Reviews.Commands.CreateHotelReview;

public sealed record CreateHotelReviewCommand(
    Guid HotelId,
    Guid BookingId,
    Guid UserId,
    short Rating,
    string? Title,
    string? Comment
) : IRequest<Result<ReviewDto>>;