using FluentValidation;

namespace HotelBooking.Application.Features.Admin.HotelRoomTypes.Commands.UpdateHotelRoomType;

public sealed class UpdateHotelRoomTypeCommandValidator : AbstractValidator<UpdateHotelRoomTypeCommand>
{
    public UpdateHotelRoomTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.PricePerNight)
            .GreaterThan(0);

        RuleFor(x => x.AdultCapacity)
            .GreaterThanOrEqualTo((short)1);

        RuleFor(x => x.ChildCapacity)
            .GreaterThanOrEqualTo((short)0);

        RuleFor(x => x.MaxOccupancy)
            .GreaterThanOrEqualTo((short)1)
            .When(x => x.MaxOccupancy.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MaxOccupancy.HasValue || x.MaxOccupancy.Value <= x.AdultCapacity + x.ChildCapacity)
            .WithMessage("MaxOccupancy cannot exceed AdultCapacity + ChildCapacity.");

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}