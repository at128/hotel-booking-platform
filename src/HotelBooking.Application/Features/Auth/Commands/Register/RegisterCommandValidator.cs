using FluentValidation;
using HotelBooking.Domain.Common.Constants;

namespace HotelBooking.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(HotelBookingConstants.FieldLengths.EmailMaxLength);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(HotelBookingConstants.FieldLengths.PasswordMinLength)
                .WithMessage($"Password must be at least {HotelBookingConstants.FieldLengths.PasswordMinLength} characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

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
