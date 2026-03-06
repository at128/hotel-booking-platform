using FluentValidation;

namespace HotelBooking.Application.Features.Admin.Rooms.Commands.UpdateRoom;

public sealed class UpdateRoomCommandValidator : AbstractValidator<UpdateRoomCommand>
{
    public UpdateRoomCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.RoomNumber)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.Floor)
            .GreaterThanOrEqualTo((short)0)
            .When(x => x.Floor.HasValue);
    }
}