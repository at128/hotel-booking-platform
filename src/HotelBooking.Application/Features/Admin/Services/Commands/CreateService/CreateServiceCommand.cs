using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Services.Commands.CreateService;

public sealed record CreateServiceCommand(
    string Name,
    string? Description)
    : IRequest<Result<ServiceDto>>;