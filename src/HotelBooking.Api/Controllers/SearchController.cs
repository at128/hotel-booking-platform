using HotelBooking.Application.Features.Search.Queries.SearchHotels;
using HotelBooking.Contracts.Search;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Controllers;

public sealed class SearchController(ISender sender) : ApiController
{
    [HttpGet("hotels")]
    public async Task<IActionResult> SearchHotels([FromQuery] SearchHotelsRequest request, CancellationToken ct)
    {
        var query = new SearchHotelsQuery(
            request.City,
            request.CheckIn,
            request.CheckOut,
            request.Adults,
            request.Children,
            request.NumberOfRooms,
            request.MinPrice,
            request.MaxPrice,
            request.MinStarRating,
            request.Amenities,
            request.SortBy,
            request.Cursor,
            request.Limit ?? 20
        );

        var result = await sender.Send(query, ct);
        return result.Match(Ok, Problem);
    }
}