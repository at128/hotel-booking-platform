using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Cart;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Rooms;
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

        var totalRooms = await db.Rooms
            .CountAsync(r =>
                r.HotelRoomTypeId == item.HotelRoomTypeId &&
                r.DeletedAtUtc == null &&
                r.Status == RoomStatus.Available, ct);

        var bookedRooms = await db.BookingRooms
            .CountAsync(br =>
                br.HotelRoomTypeId == item.HotelRoomTypeId &&
                br.Booking.Status != BookingStatus.Cancelled &&
                br.Booking.Status != BookingStatus.Failed &&
                br.Booking.CheckIn < item.CheckOut &&
                br.Booking.CheckOut > item.CheckIn, ct);

        var heldRooms = await db.CheckoutHolds
            .Where(ch =>
                ch.HotelRoomTypeId == item.HotelRoomTypeId &&
                !ch.IsReleased &&
                ch.ExpiresAtUtc > DateTimeOffset.UtcNow &&
                ch.CheckIn < item.CheckOut &&
                ch.CheckOut > item.CheckIn)
            .SumAsync(ch => (int?)ch.Quantity, ct) ?? 0;

        var siblingCartQty = await db.CartItems
            .Where(c =>
                c.Id != item.Id &&
                c.UserId == cmd.UserId &&
                c.HotelRoomTypeId == item.HotelRoomTypeId &&
                c.CheckIn == item.CheckIn &&
                c.CheckOut == item.CheckOut &&
                c.Adults == item.Adults &&
                c.Children == item.Children)
            .SumAsync(c => (int?)c.Quantity, ct) ?? 0;

        var available = totalRooms - bookedRooms - heldRooms - siblingCartQty;

        if (available < cmd.Quantity)
            return ApplicationErrors.Cart.QuantityExceedsCapacity;

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
            Adults: item.Adults,
            Children: item.Children,
            PricePerNight: item.HotelRoomType.PricePerNight,
            CheckIn: item.CheckIn,
            CheckOut: item.CheckOut,
            Nights: nights,
            Quantity: item.Quantity,
            Subtotal: item.HotelRoomType.PricePerNight * nights * item.Quantity);
    }
}
