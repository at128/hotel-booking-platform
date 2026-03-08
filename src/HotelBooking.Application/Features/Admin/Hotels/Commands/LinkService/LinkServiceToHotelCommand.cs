using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Hotels.Commands.LinkService;

public sealed record LinkServiceToHotelCommand(
    Guid HotelId,
    Guid ServiceId,
    decimal Price,
    bool IsFree
) : IRequest<Result<Success>>;