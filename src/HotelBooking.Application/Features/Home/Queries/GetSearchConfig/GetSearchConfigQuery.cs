using HotelBooking.Contracts.Home;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Home.Queries.GetSearchConfig;

public sealed record GetSearchConfigQuery() : IRequest<Result<SearchConfigResponse>>;