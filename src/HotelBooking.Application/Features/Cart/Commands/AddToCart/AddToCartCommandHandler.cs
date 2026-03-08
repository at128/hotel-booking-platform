using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Cart;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Cart;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;
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

        if (cmd.Adults < 1 || cmd.Children < 0)
            return ApplicationErrors.Cart.InvalidGuests;

        if (!roomType.CanAccommodate(cmd.Adults, cmd.Children))
            return ApplicationErrors.Cart.RoomOccupancyExceeded;

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

        var totalRooms = await db.Rooms
            .CountAsync(r =>
                r.HotelRoomTypeId == cmd.HotelRoomTypeId &&
                r.DeletedAtUtc == null &&
                r.Status == RoomStatus.Available, ct);

        var bookedRooms = await db.BookingRooms
            .CountAsync(br =>
                br.HotelRoomTypeId == cmd.HotelRoomTypeId &&
                br.Booking.Status != BookingStatus.Cancelled &&
                br.Booking.Status != BookingStatus.Failed &&
                br.Booking.CheckIn < cmd.CheckOut &&
                br.Booking.CheckOut > cmd.CheckIn, ct);

        var heldRooms = await db.CheckoutHolds
            .Where(ch =>
                ch.HotelRoomTypeId == cmd.HotelRoomTypeId &&
                !ch.IsReleased &&
                ch.ExpiresAtUtc > DateTimeOffset.UtcNow &&
                ch.CheckIn < cmd.CheckOut &&
                ch.CheckOut > cmd.CheckIn)
            .SumAsync(ch => (int?)ch.Quantity, ct) ?? 0;

        var existingCartQty = existingItems
            .Where(c =>
                c.HotelRoomTypeId == cmd.HotelRoomTypeId &&
                c.CheckIn == cmd.CheckIn &&
                c.CheckOut == cmd.CheckOut &&
                c.Adults == cmd.Adults &&
                c.Children == cmd.Children)
            .Sum(c => c.Quantity);

        var available = totalRooms - bookedRooms - heldRooms - existingCartQty;

        if (available < cmd.Quantity)
            return ApplicationErrors.Cart.QuantityExceedsCapacity;

        var existingItem = existingItems
            .FirstOrDefault(c =>
                c.HotelRoomTypeId == cmd.HotelRoomTypeId &&
                c.CheckIn == cmd.CheckIn &&
                c.CheckOut == cmd.CheckOut &&
                c.Adults == cmd.Adults &&
                c.Children == cmd.Children);

        if (existingItem is not null)
        {
            await db.CartItems
                .Where(c => c.Id == existingItem.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.Quantity, c => c.Quantity + cmd.Quantity), ct);

            var updatedItem = await db.CartItems
                .AsNoTracking()
                .FirstAsync(c => c.Id == existingItem.Id, ct);

            return MapToDto(updatedItem, roomType, nights);
        }

        var cartItem = new CartItem(
            id: Guid.CreateVersion7(),
            userId: cmd.UserId,
            hotelId: roomType.HotelId,
            hotelRoomTypeId: cmd.HotelRoomTypeId,
            checkIn: cmd.CheckIn,
            checkOut: cmd.CheckOut,
            quantity: cmd.Quantity,
            adults: cmd.Adults,
            children: cmd.Children);

        db.CartItems.Add(cartItem);

        try
        {
            await db.SaveChangesAsync(ct);
            return MapToDto(cartItem, roomType, nights);
        }
        catch (DbUpdateException ex) when (IsLikelyUniqueConstraintViolation(ex))
        {
            db.CartItems.Remove(cartItem);

            var affected = await db.CartItems
                .Where(c =>
                    c.UserId == cmd.UserId &&
                    c.HotelRoomTypeId == cmd.HotelRoomTypeId &&
                    c.CheckIn == cmd.CheckIn &&
                    c.CheckOut == cmd.CheckOut &&
                    c.Adults == cmd.Adults &&
                    c.Children == cmd.Children)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.Quantity, c => c.Quantity + cmd.Quantity), ct);

            if (affected == 0)
                throw;

            var mergedItem = await db.CartItems
                .AsNoTracking()
                .FirstAsync(c =>
                    c.UserId == cmd.UserId &&
                    c.HotelRoomTypeId == cmd.HotelRoomTypeId &&
                    c.CheckIn == cmd.CheckIn &&
                    c.CheckOut == cmd.CheckOut &&
                    c.Adults == cmd.Adults &&
                    c.Children == cmd.Children, ct);

            return MapToDto(mergedItem, roomType, nights);
        }
    }

    private static bool IsLikelyUniqueConstraintViolation(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;

        return msg.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("UNIQUE KEY", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("unique constraint", StringComparison.OrdinalIgnoreCase);
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
            Adults: item.Adults,
            Children: item.Children,
            PricePerNight: roomType.PricePerNight,
            CheckIn: item.CheckIn,
            CheckOut: item.CheckOut,
            Nights: nights,
            Quantity: item.Quantity,
            Subtotal: roomType.PricePerNight * nights * item.Quantity);
}