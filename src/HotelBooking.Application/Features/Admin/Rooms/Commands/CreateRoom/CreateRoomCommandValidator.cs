using FluentValidation;

namespace HotelBooking.Application.Features.Admin.Rooms.Commands.CreateRoom;

public sealed class CreateRoomCommandValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomCommandValidator()
    {
        RuleFor(x => x.HotelRoomTypeId)
            .NotEmpty();

        RuleFor(x => x.RoomNumber)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.Floor)
            .GreaterThanOrEqualTo((short)0)
            .When(x => x.Floor.HasValue);
    }
}