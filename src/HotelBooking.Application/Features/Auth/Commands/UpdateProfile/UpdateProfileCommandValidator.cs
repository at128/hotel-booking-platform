using FluentValidation;
using HotelBooking.Domain.Common.Constants;

namespace HotelBooking.Application.Features.Auth.Commands.UpdateProfile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(HotelBookingConstants.FieldLengths.FirstNameMaxLength);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(HotelBookingConstants.FieldLengths.LastNameMaxLength);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(HotelBookingConstants.FieldLengths.PhoneNumberMaxLength)
            .When(x => x.PhoneNumber is not null);
    }
}
