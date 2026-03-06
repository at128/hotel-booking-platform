using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Rooms;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Rooms.Commands.UpdateRoom;

public sealed record UpdateRoomCommand(
    Guid Id,
    string RoomNumber,
    short? Floor,
    RoomStatus Status)
    : IRequest<Result<RoomDto>>;