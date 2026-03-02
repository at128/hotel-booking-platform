using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Hotels;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Events.Commands.TrackHotelView;

public sealed class TrackHotelViewCommandHandler(IAppDbContext context)
    : IRequestHandler<TrackHotelViewCommand, Result<Success>>
{
    private static readonly TimeSpan DedupWindow = TimeSpan.FromMinutes(5);

    public async Task<Result<Success>> Handle(TrackHotelViewCommand cmd, CancellationToken ct)
    {
        var hotelExists = await context.Hotels
            .AnyAsync(h => h.Id == cmd.HotelId, ct);

        if (!hotelExists)
            return HotelErrors.NotFound;

        var existingVisit = await context.HotelVisits
            .Where(hv => hv.UserId == cmd.UserId && hv.HotelId == cmd.HotelId)
            .OrderByDescending(hv => hv.VisitedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (existingVisit is null)
        {
            var visit = new HotelVisit(Guid.NewGuid(), cmd.UserId, cmd.HotelId);
            visit.UpdateVisitTime();
            context.HotelVisits.Add(visit);

            await context.SaveChangesAsync(ct);
            return Result.Success;
        }

        var cutoff = DateTimeOffset.UtcNow.Subtract(DedupWindow);
        if (existingVisit.VisitedAtUtc >= cutoff)
            return Result.Success;

        existingVisit.UpdateVisitTime();
        await context.SaveChangesAsync(ct);

        return Result.Success;
    }
}