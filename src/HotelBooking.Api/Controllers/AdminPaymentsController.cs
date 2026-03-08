using HotelBooking.Application.Features.Admin.Payments.Queries.GetAdminPayments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HotelBooking.Api.Controllers;

[Authorize(Roles = "Admin")]
[EnableRateLimiting("admin")]
public sealed class AdminPaymentsController(ISender sender) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? bookingNumber,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new GetAdminPaymentsQuery(status, bookingNumber, page, pageSize), ct);
        return result.Match(Ok, Problem);
    }
}
