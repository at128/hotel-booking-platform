using FluentValidation;
using HotelBooking.Domain.Common.Constants;

namespace HotelBooking.Application.Features.Cart.Commands.UpdateCartItem;

public sealed class UpdateCartItemCommandValidator : AbstractValidator<UpdateCartItemCommand>
{
    public UpdateCartItemCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.CartItemId).NotEmpty();
        RuleFor(x => x.Quantity)
            .InclusiveBetween(
                HotelBookingConstants.Cart.MinQuantity,
                HotelBookingConstants.Cart.MaxQuantity)
            .WithMessage(
                $"Quantity must be between {HotelBookingConstants.Cart.MinQuantity} and {HotelBookingConstants.Cart.MaxQuantity}.");
    }
}
