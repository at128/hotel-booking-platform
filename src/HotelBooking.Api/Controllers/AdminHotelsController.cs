using HotelBooking.Api.Contracts.Admin;
using HotelBooking.Api.Services.Images;
using HotelBooking.Application.Features.Admin.Hotels.Command.CreateHotel;
using HotelBooking.Application.Features.Admin.Hotels.Command.DeleteHotel;
using HotelBooking.Application.Features.Admin.Hotels.Command.UpdateHotel;
using HotelBooking.Application.Features.Admin.Hotels.Commands.AddHotelImage;
using HotelBooking.Application.Features.Admin.Hotels.Commands.DeleteHotelImage;
using HotelBooking.Application.Features.Admin.Hotels.Commands.LinkService;
using HotelBooking.Application.Features.Admin.Hotels.Commands.SetHotelThumbnail;
using HotelBooking.Application.Features.Admin.Hotels.Commands.UnlinkService;
using HotelBooking.Application.Features.Admin.Hotels.Commands.UpdateHotelImage;
using HotelBooking.Application.Features.Admin.Hotels.Query.GetHotelById;
using HotelBooking.Application.Features.Admin.Hotels.Query.GetHotels;
using HotelBooking.Contracts.Admin;
using HotelBooking.Contracts.Hotels;
using HotelBooking.Domain.Common.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ImageDto = HotelBooking.Contracts.Admin.ImageDto;

namespace HotelBooking.Api.Controllers;

[Authorize(Roles = "Admin")]
[EnableRateLimiting("admin")]
public sealed class AdminHotelsController(ISender sender, IHotelImageUploadProcessor imageUploadProcessor) : ApiController
{
    private const string AdminUploadsRateLimitPolicy = "admin-uploads";
    private const long UploadRequestLimitBytes = 6 * 1024 * 1024;


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

    [HttpPost("{id:guid}/images")]
    [EnableRateLimiting(AdminUploadsRateLimitPolicy)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(UploadRequestLimitBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadRequestLimitBytes)]
    [ProducesResponseType(typeof(ImageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> UploadHotelImage(
    Guid id,
    [FromForm] UploadHotelImageForm form,
    CancellationToken ct)
    {
        var hotelResult = await sender.Send(new GetHotelByIdQuery(id), ct);
        if (hotelResult.IsError)
            return Problem(hotelResult.Errors);

        StoredHotelImageFile? stored = null;

        try
        {
            stored = await imageUploadProcessor.ProcessAndStoreAsync(id, form.Image, ct);

            var result = await sender.Send(
                new AddHotelImageCommand(
                    HotelId: id,
                    Url: stored.RelativeUrl,
                    Caption: form.Caption,
                    SortOrder: form.SortOrder),
                ct);

            if (result.IsError)
            {
                imageUploadProcessor.TryDelete(stored.AbsolutePath);
                return Problem(result.Errors);
            }

            return CreatedAtAction(
                nameof(GetHotelById),
                new { id, version = "1" },
                result.Value);
        }
        catch (ImageUploadValidationException ex)
        {
            if (stored is not null)
                imageUploadProcessor.TryDelete(stored.AbsolutePath);

            return Problem(
                statusCode: ex.StatusCode,
                title: "INVALID_IMAGE_UPLOAD",
                detail: ex.Message);
        }
        catch
        {
            if (stored is not null)
                imageUploadProcessor.TryDelete(stored.AbsolutePath);

            throw;
        }
    }

    [HttpDelete("{hotelId:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> DeleteImage(Guid hotelId, Guid imageId, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteHotelImageCommand(hotelId, imageId), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPut("{hotelId:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> UpdateImage(
        Guid hotelId, Guid imageId,
        [FromBody] UpdateImageRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateHotelImageCommand(
            hotelId, imageId, request.Caption, request.SortOrder), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPatch("{hotelId:guid}/images/{imageId:guid}/set-thumbnail")]
    public async Task<IActionResult> SetThumbnail(
        Guid hotelId, Guid imageId, CancellationToken ct)
    {
        var result = await sender.Send(new SetHotelThumbnailCommand(hotelId, imageId), ct);
        return result.Match(_ => NoContent(), Problem);
    }


    [HttpPost("{hotelId:guid}/services")]
    public async Task<IActionResult> LinkService(
    Guid hotelId, [FromBody] LinkServiceRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new LinkServiceToHotelCommand(
            hotelId, request.ServiceId, request.Price, request.IsFree), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpDelete("{hotelId:guid}/services/{serviceId:guid}")]
    public async Task<IActionResult> UnlinkService(
        Guid hotelId, Guid serviceId, CancellationToken ct)
    {
        var result = await sender.Send(
            new UnlinkServiceFromHotelCommand(hotelId, serviceId), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
