using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.HotelRoomTypes.Commands.DeleteHotelRoomType;

public sealed record DeleteHotelRoomTypeCommand(
    Guid Id) : IRequest<Result<Deleted>>;