using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Services.Commands.DeleteService;

public sealed record DeleteServiceCommand(Guid Id) : IRequest<Result<Deleted>>;