using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Cart;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Cart.Commands.UpdateCartItem;

public sealed class UpdateCartItemCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateCartItemCommand, Result<CartItemDto>>
{
    public async Task<Result<CartItemDto>> Handle(
        UpdateCartItemCommand cmd, CancellationToken ct)
    {
        var item = await db.CartItems
            .Include(c => c.Hotel)
            .Include(c => c.HotelRoomType)
                .ThenInclude(rt => rt.RoomType)
            .FirstOrDefaultAsync(
                c => c.Id == cmd.CartItemId && c.UserId == cmd.UserId, ct);

        if (item is null)
            return ApplicationErrors.Cart.CartItemNotFound;

        item.UpdateQuantity(cmd.Quantity);
        await db.SaveChangesAsync(ct);

        var nights = item.CheckOut.DayNumber - item.CheckIn.DayNumber;

        return new CartItemDto(
            Id: item.Id,
            HotelId: item.HotelId,
            HotelName: item.Hotel.Name,
            HotelRoomTypeId: item.HotelRoomTypeId,
            RoomTypeName: item.HotelRoomType.RoomType.Name,
            MaxOccupancy: item.HotelRoomType.MaxOccupancy,
            PricePerNight: item.HotelRoomType.PricePerNight,
            CheckIn: item.CheckIn,
            CheckOut: item.CheckOut,
            Nights: nights,
            Quantity: item.Quantity,
            Subtotal: item.HotelRoomType.PricePerNight * nights * item.Quantity);
    }
}