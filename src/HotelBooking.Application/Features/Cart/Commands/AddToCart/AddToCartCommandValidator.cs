using FluentValidation;

namespace HotelBooking.Application.Features.Cart.Commands.AddToCart;

public sealed class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.HotelRoomTypeId).NotEmpty();

        RuleFor(x => x.CheckIn)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.Date))
            .WithMessage("Check-in date must be today or in the future.");

        RuleFor(x => x.CheckOut)
            .GreaterThan(x => x.CheckIn)
            .WithMessage("Check-out must be after check-in.");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(1, 10)
            .WithMessage("Quantity must be between 1 and 10.");

        RuleFor(x => x.Adults)
            .InclusiveBetween(1, 20)
            .WithMessage("Adults must be between 1 and 20.");

        RuleFor(x => x.Children)
            .InclusiveBetween(0, 10)
            .WithMessage("Children must be between 0 and 10.");
    }
}