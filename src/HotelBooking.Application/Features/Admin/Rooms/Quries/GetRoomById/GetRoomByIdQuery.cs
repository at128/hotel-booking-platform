using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Rooms.Quries.GetRoomById;

public sealed record GetRoomByIdQuery(Guid Id) : IRequest<Result<RoomDto>>;