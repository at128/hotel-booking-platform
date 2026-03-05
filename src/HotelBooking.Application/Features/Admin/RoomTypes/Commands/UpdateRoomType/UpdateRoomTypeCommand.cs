using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Commands.UpdateRoomType;

public sealed record UpdateRoomTypeCommand(
    Guid Id,
    string Name,
    string? Description)
    : IRequest<Result<RoomTypeDto>>;