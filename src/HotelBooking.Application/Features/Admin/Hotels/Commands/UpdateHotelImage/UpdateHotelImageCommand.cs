using HotelBooking.Contracts.Admin;
using HotelBooking.Contracts.Hotels;
using HotelBooking.Domain.Common.Results;
using MediatR;
using ImageDto = HotelBooking.Contracts.Admin.ImageDto;

namespace HotelBooking.Application.Features.Admin.Hotels.Commands.UpdateHotelImage;

public sealed record UpdateHotelImageCommand(
    Guid HotelId,
    Guid ImageId,
    string? Caption,
    int SortOrder
) : IRequest<Result<ImageDto>>;