using HotelBooking.Application.Features.Admin.FeaturedDeals.Commands.CreateFeaturedDeal;
using HotelBooking.Application.Features.Admin.FeaturedDeals.Commands.DeleteFeaturedDeal;
using HotelBooking.Application.Features.Admin.FeaturedDeals.Commands.UpdateFeaturedDeal;
using HotelBooking.Application.Features.Admin.FeaturedDeals.Queries.GetAdminFeaturedDeals;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HotelBooking.Api.Controllers;

[Authorize(Roles = HotelBookingConstants.Roles.Admin)]
[EnableRateLimiting("admin")]
public sealed class AdminFeaturedDealsController(ISender sender) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new GetAdminFeaturedDealsQuery(search, page, pageSize), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateFeaturedDealRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateFeaturedDealCommand(
            request.HotelId, request.HotelRoomTypeId,
            request.OriginalPrice, request.DiscountedPrice,
            request.DisplayOrder, request.StartsAtUtc, request.EndsAtUtc), ct);

        return result.Match(
            deal => CreatedAtAction(nameof(GetAll), deal),
            Problem);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateFeaturedDealRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateFeaturedDealCommand(
            id, request.OriginalPrice, request.DiscountedPrice,
            request.DisplayOrder, request.StartsAtUtc, request.EndsAtUtc), ct);
        return result.Match(Ok, Problem);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteFeaturedDealCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
