using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Cart.Commands.ClearCart;

public sealed class ClearCartCommandHandler(IAppDbContext db)
    : IRequestHandler<ClearCartCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(
        ClearCartCommand cmd, CancellationToken ct)
    {
        await db.CartItems
            .Where(c => c.UserId == cmd.UserId)
            .ExecuteDeleteAsync(ct);

        return Result.Success;
    }
}