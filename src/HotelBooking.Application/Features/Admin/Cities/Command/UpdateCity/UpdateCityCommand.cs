using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Cities.Command.UpdateCity;

public sealed record UpdateCityCommand(
    Guid Id,
    string Name,
    string Country,
    string? PostOffice
) : IRequest<Result<CityDto>>;