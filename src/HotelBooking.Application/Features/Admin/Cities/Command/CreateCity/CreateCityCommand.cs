using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Application.Features.Admin.Cities.Command.CreateCity;

public sealed record CreateCityCommand(
    string Name,
    string Country,
    string? PostOffice
) : IRequest<Result<CityDto>>;