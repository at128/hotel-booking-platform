using FluentValidation;

namespace HotelBooking.Application.Features.Admin.Cities.Command.UpdateCity;

public sealed class UpdateCityCommandValidator : AbstractValidator<UpdateCityCommand>
{
    public UpdateCityCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("City name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(100);

        RuleFor(x => x.PostOffice)
            .MaximumLength(20)
            .When(x => x.PostOffice is not null);
    }
}