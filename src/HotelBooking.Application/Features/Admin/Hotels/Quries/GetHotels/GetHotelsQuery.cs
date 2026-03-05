using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Hotels.Query.GetHotels;

public sealed record GetHotelsQuery(
    Guid? CityId = null,
    string? Search = null,   // hotel name / owner / city name
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PaginatedAdminResponse<HotelDto>>>;