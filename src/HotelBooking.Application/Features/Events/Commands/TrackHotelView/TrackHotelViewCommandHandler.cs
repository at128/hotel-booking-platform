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

        var existingVisit = await LoadVisitAsync(cmd.UserId, cmd.HotelId, ct);

        if (existingVisit is null)
        {
            var visit = new HotelVisit(Guid.NewGuid(), cmd.UserId, cmd.HotelId);
            visit.UpdateVisitTime();
            context.HotelVisits.Add(visit);

            try
            {
                await context.SaveChangesAsync(ct);
                return Result.Success;
            }
            catch (DbUpdateException ex) when (IsLikelyUniqueConstraintViolation(ex))
            {
                context.HotelVisits.Remove(visit);

                existingVisit = await LoadVisitAsync(cmd.UserId, cmd.HotelId, ct);
                if (existingVisit is null)
                    throw; 
            }
        }

        var cutoff = DateTimeOffset.UtcNow.Subtract(DedupWindow);
        if (existingVisit.VisitedAtUtc >= cutoff)
            return Result.Success;

        existingVisit.UpdateVisitTime();
        await context.SaveChangesAsync(ct);

        return Result.Success;
    }

    private Task<HotelVisit?> LoadVisitAsync(Guid userId, Guid hotelId, CancellationToken ct)
    {
        return context.HotelVisits
            .Where(hv => hv.UserId == userId && hv.HotelId == hotelId)
            .OrderByDescending(hv => hv.VisitedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    private static bool IsLikelyUniqueConstraintViolation(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;

        return msg.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("UNIQUE KEY", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("unique constraint", StringComparison.OrdinalIgnoreCase);
    }
}