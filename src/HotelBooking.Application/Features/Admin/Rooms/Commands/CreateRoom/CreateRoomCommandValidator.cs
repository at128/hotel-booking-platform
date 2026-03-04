using FluentValidation;

namespace HotelBooking.Application.Features.Admin.Rooms.Command.CreateRoom;

public sealed class CreateRoomCommandValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomCommandValidator()
    {
        RuleFor(x => x.HotelId)
            .NotEmpty();

        RuleFor(x => x.RoomTypeId)
            .NotEmpty();

        RuleFor(x => x.PricePerNight)
            .GreaterThan(0);

        RuleFor(x => x.AdultCapacity)
            .GreaterThanOrEqualTo((short)0);

        RuleFor(x => x.ChildCapacity)
            .GreaterThanOrEqualTo((short)0);

        RuleFor(x => x)
            .Must(x => (x.AdultCapacity + x.ChildCapacity) > 0)
            .WithMessage("At least one occupant must be supported.");

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}