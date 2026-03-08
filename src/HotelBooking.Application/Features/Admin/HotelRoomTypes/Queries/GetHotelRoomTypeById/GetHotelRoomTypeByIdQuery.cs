using HotelBooking.Contracts.Admin.HotelRoomTypes;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.HotelRoomTypes.Queries.GetHotelRoomTypeById;

public sealed record GetHotelRoomTypeByIdQuery(
    Guid Id) : IRequest<Result<HotelRoomTypeAdminDto>>;