using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Cart;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Cart.Queries;

public sealed class GetCartQueryHandler(IAppDbContext db)
    : IRequestHandler<GetCartQuery, Result<CartResponse>>
{
    public async Task<Result<CartResponse>> Handle(
        GetCartQuery query, CancellationToken ct)
    {
        var items = await db.CartItems
            .AsNoTracking()
            .Include(c => c.Hotel)
            .Include(c => c.HotelRoomType)
                .ThenInclude(rt => rt.RoomType)
            .Where(c => c.UserId == query.UserId)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync(ct);

        if (items.Count == 0)
        {
            return new CartResponse(
                HotelId: null, HotelName: null,
                CheckIn: null, CheckOut: null,
                TotalNights: 0, Items: [], Total: 0m, TotalRooms: 0);
        }

        var first = items[0];
        var nights = first.CheckOut.DayNumber - first.CheckIn.DayNumber;

        var dtos = items.Select(item =>
        {
            var rt = item.HotelRoomType;
            var subtotal = rt.PricePerNight * nights * item.Quantity;
            return new CartItemDto(
                Id: item.Id,
                HotelId: item.HotelId,
                HotelName: item.Hotel.Name,
                HotelRoomTypeId: item.HotelRoomTypeId,
                RoomTypeName: rt.RoomType.Name,
                MaxOccupancy: rt.AdultCapacity,
                PricePerNight: rt.PricePerNight,
                CheckIn: item.CheckIn,
                CheckOut: item.CheckOut,
                Nights: nights,
                Quantity: item.Quantity,
                Subtotal: subtotal);
        }).ToList();

        return new CartResponse(
            HotelId: first.HotelId,
            HotelName: first.Hotel.Name,
            CheckIn: first.CheckIn,
            CheckOut: first.CheckOut,
            TotalNights: nights,
            Items: dtos,
            Total: dtos.Sum(d => d.Subtotal),
            TotalRooms: dtos.Sum(d => d.Quantity));
    }
}