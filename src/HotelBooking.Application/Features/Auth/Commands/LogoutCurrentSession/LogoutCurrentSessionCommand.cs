using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Auth.Commands.LogoutCurrentSession;

public sealed record LogoutCurrentSessionCommand(Guid UserId)
    : IRequest<Result<Success>>;