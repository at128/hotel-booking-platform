using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Rooms.Commands.DeleteRoom;

public sealed record DeleteRoomCommand(Guid Id) : IRequest<Result<Deleted>>;