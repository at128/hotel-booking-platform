using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Cart;
using HotelBooking.Domain.Cart;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Hotels;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Cart.Commands.AddToCart;

public sealed class AddToCartCommandHandler(IAppDbContext db)
    : IRequestHandler<AddToCartCommand, Result<CartItemDto>>
{
    public async Task<Result<CartItemDto>> Handle(
        AddToCartCommand cmd, CancellationToken ct)
    {
        var roomType = await db.HotelRoomTypes
            .Include(rt => rt.Hotel)
            .Include(rt => rt.RoomType)
            .FirstOrDefaultAsync(rt => rt.Id == cmd.HotelRoomTypeId, ct);

        if (roomType is null)
            return ApplicationErrors.Cart.RoomTypeNotFound;

        var nights = cmd.CheckOut.DayNumber - cmd.CheckIn.DayNumber;
        if (nights <= 0)
            return ApplicationErrors.Cart.InvalidDates;

        var existingItems = await db.CartItems
            .Where(c => c.UserId == cmd.UserId)
            .ToListAsync(ct);

        if (existingItems.Count > 0)
        {
            var firstItem = existingItems[0];

            if (firstItem.HotelId != roomType.HotelId)
                return ApplicationErrors.Cart.HotelMismatch;

            if (firstItem.CheckIn != cmd.CheckIn || firstItem.CheckOut != cmd.CheckOut)
                return ApplicationErrors.Cart.DateMismatch;
        }

        var existingItem = existingItems
            .FirstOrDefault(c => c.HotelRoomTypeId == cmd.HotelRoomTypeId);

        if (existingItem is not null)
        {
            var newQty = existingItem.Quantity + cmd.Quantity;
            existingItem.UpdateQuantity(newQty);
            await db.SaveChangesAsync(ct);

            return MapToDto(existingItem, roomType, nights);
        }

        var cartItem = new CartItem(
            id: Guid.CreateVersion7(),
            userId: cmd.UserId,
            hotelId: roomType.HotelId,
            hotelRoomTypeId: cmd.HotelRoomTypeId,
            checkIn: cmd.CheckIn,
            checkOut: cmd.CheckOut,
            quantity: cmd.Quantity);

        db.CartItems.Add(cartItem);
        await db.SaveChangesAsync(ct);

        return MapToDto(cartItem, roomType, nights);
    }

    private static CartItemDto MapToDto(
        CartItem item,
        HotelRoomType roomType,
        int nights) => new(
            Id: item.Id,
            HotelId: item.HotelId,
            HotelName: roomType.Hotel.Name,
            HotelRoomTypeId: item.HotelRoomTypeId,
            RoomTypeName: roomType.RoomType.Name,
            MaxOccupancy: roomType.MaxOccupancy,
            PricePerNight: roomType.PricePerNight,
            CheckIn: item.CheckIn,
            CheckOut: item.CheckOut,
            Nights: nights,
            Quantity: item.Quantity,
            Subtotal: roomType.PricePerNight * nights * item.Quantity);
}