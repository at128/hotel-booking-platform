using HotelBooking.Contracts.Hotels;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Hotels.Commands.AddHotelImage;

public sealed record AddHotelImageCommand(
    Guid HotelId,
    string Url,
    string? Caption,
    int? SortOrder
) : IRequest<Result<ImageDto>>;