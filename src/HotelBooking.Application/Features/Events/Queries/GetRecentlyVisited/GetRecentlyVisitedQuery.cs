using HotelBooking.Contracts.Events;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Events.Queries.GetRecentlyVisited;

public sealed record GetRecentlyVisitedQuery(Guid UserId) : IRequest<Result<RecentlyVisitedResponse>>;