using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Rooms.Commands.UpdateRoom;

public sealed record UpdateRoomCommand(
    Guid Id,
    decimal PricePerNight,
    short AdultCapacity,
    short ChildCapacity,
    string? Description)
    : IRequest<Result<RoomDto>>;