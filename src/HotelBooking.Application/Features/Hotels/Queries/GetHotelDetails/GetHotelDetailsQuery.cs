using HotelBooking.Contracts.Hotels;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Hotels.Queries.GetHotelDetails;

public sealed record GetHotelDetailsQuery(Guid HotelId) : IRequest<Result<HotelDetailsDto>>;