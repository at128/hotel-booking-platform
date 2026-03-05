using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Cities.Query.GetCityById;

public sealed record GetCityByIdQuery(Guid Id)
    : IRequest<Result<CityDto>>;