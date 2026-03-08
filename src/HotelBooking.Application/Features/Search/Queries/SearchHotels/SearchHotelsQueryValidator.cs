using FluentValidation;
using HotelBooking.Application.Features.Search.Queries.SearchHotels;

public sealed class SearchHotelsQueryValidator : AbstractValidator<SearchHotelsQuery>
{
    public SearchHotelsQueryValidator()
    {
        RuleFor(x => x.CheckOut)
            .GreaterThan(x => x.CheckIn)
            .When(x => x.CheckIn.HasValue && x.CheckOut.HasValue)
            .WithMessage("Check-out must be after check-in.");

        RuleFor(x => x.CheckIn)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.CheckIn.HasValue)
            .WithMessage("Check-in cannot be in the past.");

        RuleFor(x => x.Adults)
            .GreaterThan(0).When(x => x.Adults.HasValue);

        RuleFor(x => x.Children)
            .GreaterThanOrEqualTo(0).When(x => x.Children.HasValue);

        RuleFor(x => x.NumberOfRooms)
            .GreaterThan(0).When(x => x.NumberOfRooms.HasValue);

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50);
    }
}