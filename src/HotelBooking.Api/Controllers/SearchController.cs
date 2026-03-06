using HotelBooking.Application.Features.Search.Queries.SearchHotels;
using HotelBooking.Contracts.Search;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Controllers;

public sealed class SearchController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(SearchHotelsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchHotels(
        [FromQuery] SearchHotelsRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new SearchHotelsQuery(
                request.Query,
                request.City,
                request.RoomTypeId,
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
                request.Limit),
            ct);

        return result.Match(Ok, Problem);
    }
}