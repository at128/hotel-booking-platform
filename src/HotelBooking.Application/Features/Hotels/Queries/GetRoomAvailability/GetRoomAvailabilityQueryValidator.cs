using FluentValidation;

namespace HotelBooking.Application.Features.Hotels.Queries.GetRoomAvailability;

public sealed class GetRoomAvailabilityQueryValidator
    : AbstractValidator<GetRoomAvailabilityQuery>
{
    public GetRoomAvailabilityQueryValidator()
    {
        RuleFor(x => x.HotelId).NotEmpty();
        RuleFor(x => x.CheckIn).NotEmpty()
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.Date))
            .WithMessage("CheckIn must be today or later");
        RuleFor(x => x.CheckOut).NotEmpty()
            .GreaterThan(x => x.CheckIn)
            .WithMessage("CheckOut must be after CheckIn");
    }
}