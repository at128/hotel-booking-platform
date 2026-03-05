using FluentValidation;

namespace HotelBooking.Application.Features.Checkout.Commands.CreateBooking;

public sealed class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.UserEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.HoldIds)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("At least one checkout hold is required.")
            .Must(ids => ids.Count <= 20).WithMessage("Too many holds in one booking.");
    }
}