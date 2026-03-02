using HotelBooking.Contracts.Hotels;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Hotels.Queries.GetHotelGallery;

public sealed record GetHotelGalleryQuery(Guid HotelId) : IRequest<Result<HotelGalleryResponse>>;