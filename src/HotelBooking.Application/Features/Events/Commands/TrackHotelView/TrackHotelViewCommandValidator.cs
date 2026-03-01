using FluentValidation;

namespace HotelBooking.Application.Features.Events.Commands.TrackHotelView;

public sealed class TrackHotelViewCommandValidator : AbstractValidator<TrackHotelViewCommand>
{
    public TrackHotelViewCommandValidator()
    {
        RuleFor(x => x.HotelId).NotEmpty().WithMessage("HotelId is required.");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
    }
}