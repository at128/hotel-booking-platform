using HotelBooking.Contracts.Admin.HotelRoomTypes;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.HotelRoomTypes.Queries.GetHotelRoomTypes;

public sealed record GetHotelRoomTypesQuery(
    Guid? HotelId = null) : IRequest<Result<List<HotelRoomTypeAdminDto>>>;