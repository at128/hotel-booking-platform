using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Settings;
using HotelBooking.Contracts.Checkout;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HotelBooking.Application.Features.Checkout.Commands.CreateCheckoutHold;

public sealed class CreateCheckoutHoldCommandHandler(
    IAppDbContext db,
    ICheckoutHoldRepository holdRepository,
    IOptions<BookingSettings> settings)
    : IRequestHandler<CreateCheckoutHoldCommand, Result<CheckoutHoldResponse>>
{
    private readonly BookingSettings _settings = settings.Value;

    public async Task<Result<CheckoutHoldResponse>> Handle(
        CreateCheckoutHoldCommand cmd, CancellationToken ct)
    {
        var cartItems = await db.CartItems
            .AsNoTracking()
            .Include(c => c.HotelRoomType)
            .Where(c => c.UserId == cmd.UserId)
            .ToListAsync(ct);

        if (cartItems.Count == 0)
            return ApplicationErrors.Checkout.CartEmpty;

        await holdRepository.ReleaseHoldsAsync(cmd.UserId, ct);

        var holdRequests = cartItems
                            .GroupBy(item => new
                            {
                                item.HotelId,
                                item.HotelRoomTypeId,
                                item.CheckIn,
                                item.CheckOut
                            })
                            .Select(g => new HoldRequest(
                                HotelId: g.Key.HotelId,
                                HotelRoomTypeId: g.Key.HotelRoomTypeId,
                                CheckIn: g.Key.CheckIn,
                                CheckOut: g.Key.CheckOut,
                                Quantity: g.Sum(x => x.Quantity)))
                            .ToList();

        var holdDuration = TimeSpan.FromMinutes(_settings.CheckoutHoldMinutes);

        var result = await holdRepository.TryAcquireHoldsAsync(
            cmd.UserId, holdRequests, holdDuration, ct);

        if (!result.IsSuccess)
            return ApplicationErrors.Checkout.RoomUnavailable(result.FailedRoomTypeName!);

        var first = cartItems[0];
        var nights = first.CheckOut.DayNumber - first.CheckIn.DayNumber;
        var subtotal = cartItems.Sum(i => i.HotelRoomType.PricePerNight * nights * i.Quantity);
        var taxRate = _settings.TaxRate;
        var tax = subtotal * taxRate;
        var total = subtotal + tax;

        return new CheckoutHoldResponse(
            HoldIds: result.HoldIds,
            HotelId: first.HotelId,
            CheckIn: first.CheckIn,
            CheckOut: first.CheckOut,
            Nights: nights,
            Subtotal: subtotal,
            Tax: tax,
            Total: total,
            ExpiresAtUtc: DateTimeOffset.UtcNow.Add(holdDuration),
            Notes: cmd.Notes);
    }
}