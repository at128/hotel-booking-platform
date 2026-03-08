using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.HotelRoomTypes.Commands.UpdateHotelRoomType;

public sealed record UpdateHotelRoomTypeCommand(
    Guid Id,
    decimal PricePerNight,
    short AdultCapacity,
    short ChildCapacity,
    short? MaxOccupancy,
    string? Description) : IRequest<Result<Updated>>;