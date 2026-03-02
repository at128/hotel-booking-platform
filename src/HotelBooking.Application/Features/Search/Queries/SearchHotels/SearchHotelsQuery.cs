using HotelBooking.Contracts.Search;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Search.Queries.SearchHotels;

public sealed record SearchHotelsQuery(
    string? City,
    DateOnly? CheckIn,
    DateOnly? CheckOut,
    int? Adults,
    int? Children,
    int? NumberOfRooms,
    decimal? MinPrice,
    decimal? MaxPrice,
    int? MinStarRating,
    List<string>? Amenities,
    string? SortBy,
    string? Cursor,
    int Limit
) : IRequest<Result<SearchHotelsResponse>>;