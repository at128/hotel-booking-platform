using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Hotels.Query.GetHotelById;

public sealed record GetHotelByIdQuery(Guid Id)
    : IRequest<Result<HotelDto>>;