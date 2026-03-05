using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Rooms.Quries.GetRooms;

public sealed record GetRoomsQuery(
    Guid? HotelId,
    Guid? RoomTypeId,
    string? Search,
    int Page = 1,
    int PageSize = 20)
    : IRequest<Result<PaginatedAdminResponse<RoomDto>>>;