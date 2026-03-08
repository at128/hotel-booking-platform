using Asp.Versioning;
using HotelBooking.Application.Features.Admin.HotelRoomTypes.Commands.CreateHotelRoomType;
using HotelBooking.Application.Features.Admin.HotelRoomTypes.Commands.DeleteHotelRoomType;
using HotelBooking.Application.Features.Admin.HotelRoomTypes.Commands.UpdateHotelRoomType;
using HotelBooking.Application.Features.Admin.HotelRoomTypes.Queries.GetHotelRoomTypeById;
using HotelBooking.Application.Features.Admin.HotelRoomTypes.Queries.GetHotelRoomTypes;
using HotelBooking.Contracts.Admin.HotelRoomTypes;
using HotelBooking.Domain.Common.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HotelBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/hotel-room-types")]
[Authorize(Roles = HotelBookingConstants.Roles.Admin)]
[EnableRateLimiting("admin")]
public sealed class AdminHotelRoomTypesController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<HotelRoomTypeAdminDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? hotelId,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetHotelRoomTypesQuery(hotelId), ct);

        if (result.IsError)
            return Problem(result.Errors);

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(HotelRoomTypeAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetHotelRoomTypeByIdQuery(id), ct);

        if (result.IsError)
            return Problem(result.Errors);

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateHotelRoomTypeRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new CreateHotelRoomTypeCommand(
            HotelId: request.HotelId,
            RoomTypeId: request.RoomTypeId,
            PricePerNight: request.PricePerNight,
            AdultCapacity: request.AdultCapacity,
            ChildCapacity: request.ChildCapacity,
            MaxOccupancy: request.MaxOccupancy,
            Description: request.Description), ct);

        if (result.IsError)
            return Problem(result.Errors);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value, version = "1.0" },
            result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateHotelRoomTypeRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new UpdateHotelRoomTypeCommand(
            Id: id,
            PricePerNight: request.PricePerNight,
            AdultCapacity: request.AdultCapacity,
            ChildCapacity: request.ChildCapacity,
            MaxOccupancy: request.MaxOccupancy,
            Description: request.Description), ct);

        if (result.IsError)
            return Problem(result.Errors);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken ct)
    {
        var result = await sender.Send(new DeleteHotelRoomTypeCommand(id), ct);

        if (result.IsError)
            return Problem(result.Errors);

        return NoContent();
    }
}
