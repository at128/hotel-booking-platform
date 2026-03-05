using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Queries.GetRoomTypeById;

public sealed record GetRoomTypeByIdQuery(Guid Id)
    : IRequest<Result<RoomTypeDto>>;