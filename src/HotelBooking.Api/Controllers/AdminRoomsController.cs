using HotelBooking.Application.Features.Admin.Rooms.Commands.CreateRoom;
using HotelBooking.Application.Features.Admin.Rooms.Commands.DeleteRoom;
using HotelBooking.Application.Features.Admin.Rooms.Commands.UpdateRoom;
using HotelBooking.Application.Features.Admin.Rooms.Quries.GetRoomById;
using HotelBooking.Application.Features.Admin.Rooms.Quries.GetRooms;
using HotelBooking.Contracts.Admin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HotelBooking.Api.Controllers;

[Authorize(Roles = "Admin")]
[EnableRateLimiting("admin")]
public sealed class AdminRoomsController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedAdminResponse<RoomDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRooms(
        [FromQuery] Guid? hotelId,
        [FromQuery] Guid? roomTypeId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new GetRoomsQuery(hotelId, roomTypeId, search, page, pageSize), ct);

        return result.Match(Ok, Problem);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoomById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetRoomByIdQuery(id), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRoom(
        [FromBody] CreateRoomRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateRoomCommand(
                request.HotelRoomTypeId,
                request.RoomNumber,
                request.Floor,
                request.Status),
            ct);

        return result.Match(
            room => CreatedAtAction(nameof(GetRoomById), new { id = room.Id, version = "1" }, room),
            Problem);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateRoom(
        Guid id,
        [FromBody] UpdateRoomRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateRoomCommand(
                id,
                request.RoomNumber,
                request.Floor,
                request.Status),
            ct);

        return result.Match(Ok, Problem);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteRoom(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteRoomCommand(id), ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }
}
