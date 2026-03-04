using HotelBooking.Application.Features.Admin.Hotels.Command.CreateHotel;
using HotelBooking.Application.Features.Admin.Hotels.Command.DeleteHotel;
using HotelBooking.Application.Features.Admin.Hotels.Command.UpdateHotel;
using HotelBooking.Application.Features.Admin.Hotels.Query.GetHotelById;
using HotelBooking.Application.Features.Admin.Hotels.Query.GetHotels;
using HotelBooking.Contracts.Admin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminHotelsController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedAdminResponse<HotelDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHotels(
        [FromQuery] Guid? cityId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new GetHotelsQuery(cityId, search, page, pageSize), ct);

        return result.Match(Ok, Problem);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(HotelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHotelById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetHotelByIdQuery(id), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost]
    [ProducesResponseType(typeof(HotelDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateHotel(
        [FromBody] CreateHotelRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateHotelCommand(
                request.CityId,
                request.Name,
                request.Owner,
                request.Address,
                request.StarRating,
                request.Description,
                request.Latitude,
                request.Longitude),
            ct);

        return result.Match(
            hotel => CreatedAtAction(nameof(GetHotelById), new { id = hotel.Id, version = "1" }, hotel),
            Problem);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(HotelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateHotel(
        Guid id,
        [FromBody] UpdateHotelRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateHotelCommand(
                id,
                request.CityId,
                request.Name,
                request.Owner,
                request.Address,
                request.StarRating,
                request.Description,
                request.Latitude,
                request.Longitude),
            ct);

        return result.Match(Ok, Problem);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteHotel(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteHotelCommand(id), ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }
}