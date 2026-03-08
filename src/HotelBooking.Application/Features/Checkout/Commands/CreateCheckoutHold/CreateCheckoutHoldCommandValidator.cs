using FluentValidation;

namespace HotelBooking.Application.Features.Checkout.Commands.CreateCheckoutHold;

public sealed class CreateCheckoutHoldCommandValidator : AbstractValidator<CreateCheckoutHoldCommand>
{
    public CreateCheckoutHoldCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}
