using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Queries.GetRoomTypes;

public sealed record GetRoomTypesQuery(
    string? Search,
    int Page = 1,
    int PageSize = 20)
    : IRequest<Result<PaginatedAdminResponse<RoomTypeDto>>>;