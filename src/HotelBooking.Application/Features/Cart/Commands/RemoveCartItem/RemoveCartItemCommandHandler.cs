using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Cart.Commands.RemoveCartItem;

public sealed class RemoveCartItemCommandHandler(IAppDbContext db)
    : IRequestHandler<RemoveCartItemCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(
        RemoveCartItemCommand cmd, CancellationToken ct)
    {
        var affected = await db.CartItems
            .Where(c => c.Id == cmd.CartItemId && c.UserId == cmd.UserId)
            .ExecuteDeleteAsync(ct);

        if (affected == 0)
            return ApplicationErrors.Cart.CartItemNotFound;

        return Result.Success;
    }
}