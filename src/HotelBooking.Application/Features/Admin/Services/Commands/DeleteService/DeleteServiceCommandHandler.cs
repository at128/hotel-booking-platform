using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Services.Commands.DeleteService;

public sealed class DeleteServiceCommandHandler(IAppDbContext db)
    : IRequestHandler<DeleteServiceCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteServiceCommand cmd, CancellationToken ct)
    {
        var entity = await db.Services
            .FirstOrDefaultAsync(s => s.Id == cmd.Id && s.DeletedAtUtc == null, ct);

        if (entity is null)
            return AdminErrors.Services.NotFound(cmd.Id);

        var hasAssignments = await db.HotelServices
            .AsNoTracking()
            .AnyAsync(hs => hs.ServiceId == cmd.Id, ct);

        if (hasAssignments)
            return AdminErrors.Services.HasRelatedHotelAssignments;

        entity.DeletedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result.Deleted;
    }
}