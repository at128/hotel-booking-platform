using HotelBooking.Application.Features.Admin.Cities.Command.CreateCity;
using HotelBooking.Application.Features.Admin.Cities.Command.DeleteCity;
using HotelBooking.Application.Features.Admin.Cities.Command.UpdateCity;
using HotelBooking.Application.Features.Admin.Cities.Queries.GetCities;
using HotelBooking.Application.Features.Admin.Cities.Query.GetCityById;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HotelBooking.Api.Controllers;

[Authorize(Roles = HotelBookingConstants.Roles.Admin)]
[EnableRateLimiting("admin")]
public sealed class AdminCitiesController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedAdminResponse<CityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCities(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetCitiesQuery(search, page, pageSize), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCity(
        [FromBody] CreateCityRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateCityCommand(request.Name, request.Country, request.PostOffice), ct);

        return result.Match(
            city => CreatedAtAction(nameof(GetCityById), new { id = city.Id, version = "1" }, city),
            Problem);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCityById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetCityByIdQuery(id), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCity(
        Guid id,
        [FromBody] UpdateCityRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateCityCommand(id, request.Name, request.Country, request.PostOffice), ct);

        return result.Match(Ok, Problem);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCity(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteCityCommand(id), ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }
}
