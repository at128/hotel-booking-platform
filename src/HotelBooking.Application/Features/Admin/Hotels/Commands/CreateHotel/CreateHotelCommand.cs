using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Hotels.Command.CreateHotel;

public sealed record CreateHotelCommand(
    Guid CityId,
    string Name,
    string Owner,
    string Address,
    short StarRating,
    string? Description,
    decimal? Latitude,
    decimal? Longitude
) : IRequest<Result<HotelDto>>;