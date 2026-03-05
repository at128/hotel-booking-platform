using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Services.Commands.UpdateService;

public sealed record UpdateServiceCommand(
    Guid Id,
    string Name,
    string? Description)
    : IRequest<Result<ServiceDto>>;