using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Auth.Commands.LogoutAllSessions;

public sealed record LogoutAllSessionsCommand(Guid UserId)
    : IRequest<Result<Success>>;