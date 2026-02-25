using HotelBooking.Contracts.Hotels;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Hotels.Queries.GetRoomAvailability;

public sealed record GetRoomAvailabilityQuery(
    Guid HotelId,
    DateOnly CheckIn,
    DateOnly CheckOut
) : IRequest<Result<RoomAvailabilityResponse>>;