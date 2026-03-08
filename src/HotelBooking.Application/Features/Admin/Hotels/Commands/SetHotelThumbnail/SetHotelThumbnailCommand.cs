using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Hotels.Commands.SetHotelThumbnail;

public sealed record SetHotelThumbnailCommand(Guid HotelId, Guid ImageId)
    : IRequest<Result<Success>>;