using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Application.Features.Cart.Commands.UpdateCartItem
{
    public class UpdateCartItemCommandValidator : AbstractValidator<UpdateCartItemCommand>
    {
        public UpdateCartItemCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.CartItemId).NotEmpty();
            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithErrorCode("InvalidQuantity");
        }
    }
}
