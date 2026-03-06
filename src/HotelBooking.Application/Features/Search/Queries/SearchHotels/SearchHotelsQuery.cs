using HotelBooking.Contracts.Search;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Search.Queries.SearchHotels;

public sealed record SearchHotelsQuery(
    string? Query,
    string? City,
    DateOnly? CheckIn,
    DateOnly? CheckOut,
    int? Adults,
    int? Children,
    int? NumberOfRooms,
    decimal? MinPrice,
    decimal? MaxPrice,
    short? MinStarRating,
    IReadOnlyCollection<string>? Amenities,
    string? SortBy,
    string? Cursor,
    int Limit = 20)
    : IRequest<Result<SearchHotelsResponse>>;