using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Rooms.Commands.CreateRoom;

public sealed record CreateRoomCommand(
    Guid HotelId,
    Guid RoomTypeId,
    decimal PricePerNight,
    short AdultCapacity,
    short ChildCapacity,
    string? Description)
    : IRequest<Result<RoomDto>>;