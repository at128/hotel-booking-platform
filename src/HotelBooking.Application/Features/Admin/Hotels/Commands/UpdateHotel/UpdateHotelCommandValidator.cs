using FluentValidation;

namespace HotelBooking.Application.Features.Admin.Hotels.Command.UpdateHotel;

public sealed class UpdateHotelCommandValidator : AbstractValidator<UpdateHotelCommand>
{
    public UpdateHotelCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.CityId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Hotel name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Owner)
            .NotEmpty().WithMessage("Owner is required.")
            .MaximumLength(200);

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(500);

        RuleFor(x => x.StarRating)
            .InclusiveBetween((short)1, (short)5);

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description is not null);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m)
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m)
            .When(x => x.Longitude.HasValue);
    }
}