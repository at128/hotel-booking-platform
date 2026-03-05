using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Services.Commands.UpdateService;

public sealed class UpdateServiceCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateServiceCommand, Result<ServiceDto>>
{
    public async Task<Result<ServiceDto>> Handle(UpdateServiceCommand cmd, CancellationToken ct)
    {
        var entity = await db.Services
            .Include(s => s.HotelServices)
            .FirstOrDefaultAsync(s => s.Id == cmd.Id && s.DeletedAtUtc == null, ct);

        if (entity is null)
            return AdminErrors.Services.NotFound(cmd.Id);

        var name = cmd.Name.Trim();
        var description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim();

        var exists = await db.Services
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(s => s.Id != cmd.Id && s.Name == name, ct);

        if (exists)
            return AdminErrors.Services.AlreadyExists;

        entity.Update(name, description);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsServiceNameUniqueViolation(ex))
        {
            return AdminErrors.Services.AlreadyExists;
        }

        return new ServiceDto(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.HotelServices.Count,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);
    }

    private static bool IsServiceNameUniqueViolation(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;

        return msg.Contains("IX_services_Name", StringComparison.OrdinalIgnoreCase) ||
               (msg.Contains("services", StringComparison.OrdinalIgnoreCase) &&
                msg.Contains("unique", StringComparison.OrdinalIgnoreCase));
    }
}