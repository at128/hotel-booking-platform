using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Commands.DeleteRoomType;

public sealed record DeleteRoomTypeCommand(Guid Id) : IRequest<Result<Deleted>>;