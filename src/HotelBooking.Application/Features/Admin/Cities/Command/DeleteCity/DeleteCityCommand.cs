using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Cities.Command.DeleteCity;

public sealed record DeleteCityCommand(Guid Id) : IRequest<Result<Deleted>>;