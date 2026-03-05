using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Admin.Hotels.Command.DeleteHotel;

public sealed record DeleteHotelCommand(Guid Id) : IRequest<Result<Deleted>>;