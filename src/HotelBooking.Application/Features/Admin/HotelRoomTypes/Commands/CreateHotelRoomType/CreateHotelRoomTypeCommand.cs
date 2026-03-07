using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.HotelRoomTypes.Commands.CreateHotelRoomType;

public sealed record CreateHotelRoomTypeCommand(
    Guid HotelId,
    Guid RoomTypeId,
    decimal PricePerNight,
    short AdultCapacity,
    short ChildCapacity,
    short? MaxOccupancy,
    string? Description) : IRequest<Result<Guid>>;