using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Reviews.Commands.DeleteReview;

public sealed record DeleteReviewCommand(
    Guid HotelId,
    Guid ReviewId,
    Guid UserId,
    bool IsAdmin
) : IRequest<Result<Deleted>>;
