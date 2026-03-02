using FluentValidation;

namespace HotelBooking.Application.Features.Search.Queries.SearchHotels;

public sealed class SearchHotelsQueryValidator : AbstractValidator<SearchHotelsQuery>
{
    public SearchHotelsQueryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50)
            .WithMessage("Limit must be between 1 and 50.");

        RuleFor(x => x.MinStarRating)
            .InclusiveBetween(1, 5)
            .When(x => x.MinStarRating.HasValue)
            .WithMessage("MinStarRating must be between 1 and 5.");

        RuleFor(x => x.MinPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinPrice.HasValue);

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaxPrice.HasValue);

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(x => x.MinPrice!.Value)
            .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue)
            .WithMessage("MaxPrice must be >= MinPrice.");

        RuleFor(x => x.CheckOut)
            .GreaterThan(x => x.CheckIn!.Value)
            .When(x => x.CheckIn.HasValue && x.CheckOut.HasValue)
            .WithMessage("CheckOut must be after CheckIn.");

        RuleFor(x => x.CheckIn)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.Date))
            .When(x => x.CheckIn.HasValue)
            .WithMessage("CheckIn cannot be in the past.");

        RuleFor(x => x.CheckOut)
            .NotNull()
            .When(x => x.CheckIn.HasValue)
            .WithMessage("CheckOut is required when CheckIn is provided.");

        RuleFor(x => x.CheckIn)
            .NotNull()
            .When(x => x.CheckOut.HasValue)
            .WithMessage("CheckIn is required when CheckOut is provided.");

        RuleFor(x => x.NumberOfRooms)
            .InclusiveBetween(1, 10)
            .When(x => x.NumberOfRooms.HasValue)
            .WithMessage("NumberOfRooms must be between 1 and 10.");

        RuleFor(x => x.SortBy)
            .Must(s => s is null or "price_asc" or "price_desc" or "rating_desc" or "stars_desc")
            .WithMessage("SortBy must be: price_asc, price_desc, rating_desc, stars_desc.");
    }
}