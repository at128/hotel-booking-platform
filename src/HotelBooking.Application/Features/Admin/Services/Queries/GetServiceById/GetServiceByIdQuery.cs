using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Services.Queries.GetServiceById;

public sealed record GetServiceByIdQuery(Guid Id)
    : IRequest<Result<ServiceDto>>;