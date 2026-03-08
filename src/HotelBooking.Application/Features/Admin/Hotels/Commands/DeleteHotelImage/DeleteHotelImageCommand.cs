using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Hotels.Commands.DeleteHotelImage;

public sealed record DeleteHotelImageCommand(Guid HotelId, Guid ImageId) : IRequest<Result<Deleted>>;