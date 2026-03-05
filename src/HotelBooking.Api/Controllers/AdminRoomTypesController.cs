using HotelBooking.Application.Features.Admin.RoomTypes.Commands.CreateRoomType;
using HotelBooking.Application.Features.Admin.RoomTypes.Commands.DeleteRoomType;
using HotelBooking.Application.Features.Admin.RoomTypes.Commands.UpdateRoomType;
using HotelBooking.Application.Features.Admin.RoomTypes.Queries.GetRoomTypeById;
using HotelBooking.Application.Features.Admin.RoomTypes.Queries.GetRoomTypes;
using HotelBooking.Contracts.Admin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminRoomTypesController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedAdminResponse<RoomTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoomTypes(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetRoomTypesQuery(search, page, pageSize), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoomTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoomTypeById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetRoomTypeByIdQuery(id), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost]
    [ProducesResponseType(typeof(RoomTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRoomType(
        [FromBody] CreateRoomTypeRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateRoomTypeCommand(request.Name, request.Description), ct);

        return result.Match(
            item => CreatedAtAction(nameof(GetRoomTypeById), new { id = item.Id, version = "1" }, item),
            Problem);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RoomTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateRoomType(
        Guid id,
        [FromBody] UpdateRoomTypeRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateRoomTypeCommand(id, request.Name, request.Description), ct);

        return result.Match(Ok, Problem);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteRoomType(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteRoomTypeCommand(id), ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }
}