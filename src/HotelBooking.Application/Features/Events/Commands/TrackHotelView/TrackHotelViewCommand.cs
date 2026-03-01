using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Events.Commands.TrackHotelView;

public sealed record TrackHotelViewCommand(Guid UserId, Guid HotelId) : IRequest<Result<Success>>;