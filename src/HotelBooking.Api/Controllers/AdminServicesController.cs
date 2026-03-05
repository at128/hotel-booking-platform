using HotelBooking.Application.Features.Admin.Services.Commands.CreateService;
using HotelBooking.Application.Features.Admin.Services.Commands.DeleteService;
using HotelBooking.Application.Features.Admin.Services.Commands.UpdateService;
using HotelBooking.Application.Features.Admin.Services.Queries.GetServiceById;
using HotelBooking.Application.Features.Admin.Services.Queries.GetServices;
using HotelBooking.Contracts.Admin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminServicesController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedAdminResponse<ServiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServices(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetServicesQuery(search, page, pageSize), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetServiceById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetServiceByIdQuery(id), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateService(
        [FromBody] CreateServiceRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateServiceCommand(request.Name, request.Description), ct);

        return result.Match(
            item => CreatedAtAction(nameof(GetServiceById), new { id = item.Id, version = "1" }, item),
            Problem);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateService(
        Guid id,
        [FromBody] UpdateServiceRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateServiceCommand(id, request.Name, request.Description), ct);

        return result.Match(Ok, Problem);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteService(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteServiceCommand(id), ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }
}