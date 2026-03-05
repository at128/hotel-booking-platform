using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;
namespace HotelBooking.Application.Features.Admin.Cities.Queries.GetCities;
public sealed record GetCitiesQuery(
    string? Search,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PaginatedAdminResponse<CityDto>>>;