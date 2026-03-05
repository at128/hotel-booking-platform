using FluentValidation;
using HotelBooking.Domain.Common.Constants;

namespace HotelBooking.Application.Features.Admin.Hotels.Commands.AddHotelImage;

public sealed class AddHotelImageCommandValidator : AbstractValidator<AddHotelImageCommand>
{
    public AddHotelImageCommandValidator()
    {
        RuleFor(x => x.HotelId)
            .NotEmpty();

        RuleFor(x => x.Url)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Caption)
            .MaximumLength(HotelBookingConstants.Image.CaptionMaxLength)
            .When(x => x.Caption is not null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0)
            .When(x => x.SortOrder.HasValue);
    }
}