using FluentValidation;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Commands.CreateRoomType;

public sealed class CreateRoomTypeCommandValidator : AbstractValidator<CreateRoomTypeCommand>
{
    public CreateRoomTypeCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Room type name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description is not null);
    }
}