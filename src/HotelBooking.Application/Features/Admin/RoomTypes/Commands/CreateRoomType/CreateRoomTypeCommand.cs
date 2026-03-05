using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Commands.CreateRoomType;

public sealed record CreateRoomTypeCommand(
    string Name,
    string? Description)
    : IRequest<Result<RoomTypeDto>>;