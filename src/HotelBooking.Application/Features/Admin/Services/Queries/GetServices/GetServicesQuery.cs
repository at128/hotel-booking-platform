using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Services.Queries.GetServices;

public sealed record GetServicesQuery(
    string? Search,
    int Page = 1,
    int PageSize = 20)
    : IRequest<Result<PaginatedAdminResponse<ServiceDto>>>;