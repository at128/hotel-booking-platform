using FluentValidation;
using HotelBooking.Application.Features.Search.Queries.SearchHotels;

public sealed class SearchHotelsQueryValidator : AbstractValidator<SearchHotelsQuery>
{
    private static readonly string[] AllowedSortBy =
    [
        "price_asc",
        "price_desc",
        "rating_desc",
        "stars_desc"
    ];

    public SearchHotelsQueryValidator()
    {
        RuleFor(x => x.CheckOut)
            .NotNull()
            .When(x => x.CheckIn.HasValue)
            .WithMessage("Check-out is required when check-in is provided.");

        RuleFor(x => x.CheckIn)
            .NotNull()
            .When(x => x.CheckOut.HasValue)
            .WithMessage("Check-in is required when check-out is provided.");

        RuleFor(x => x.CheckOut)
            .GreaterThan(x => x.CheckIn)
            .When(x => x.CheckIn.HasValue && x.CheckOut.HasValue)
            .WithMessage("Check-out must be after check-in.");

        RuleFor(x => x.CheckIn)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.CheckIn.HasValue)
            .WithMessage("Check-in cannot be in the past.");

        RuleFor(x => x.Adults)
            .InclusiveBetween(1, 20)
            .When(x => x.Adults.HasValue);

        RuleFor(x => x.Children)
            .InclusiveBetween(0, 10)
            .When(x => x.Children.HasValue);

        RuleFor(x => x.NumberOfRooms)
            .GreaterThan(0).When(x => x.NumberOfRooms.HasValue);

        RuleFor(x => x.MinStarRating)
            .InclusiveBetween((short)1, (short)5)
            .When(x => x.MinStarRating.HasValue);

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(x => x.MinPrice!.Value)
            .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue);

        RuleFor(x => x.SortBy)
            .Must(s => AllowedSortBy.Contains(s!.Trim().ToLowerInvariant()))
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy))
            .WithMessage("Invalid sort field.");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50);
    }
}
