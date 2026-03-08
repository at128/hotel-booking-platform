using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Hotels.Commands.UnlinkService;

public sealed record UnlinkServiceFromHotelCommand(
    Guid HotelId, Guid ServiceId) : IRequest<Result<Deleted>>;