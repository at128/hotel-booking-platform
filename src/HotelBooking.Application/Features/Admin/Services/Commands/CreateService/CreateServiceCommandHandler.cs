using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using DomainService = HotelBooking.Domain.Services.Service;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Services.Commands.CreateService;

public sealed class CreateServiceCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateServiceCommand, Result<ServiceDto>>
{
    public async Task<Result<ServiceDto>> Handle(CreateServiceCommand cmd, CancellationToken ct)
    {
        var name = cmd.Name.Trim();
        var description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim();

        // Important with soft-delete + unique index on Name
        var exists = await db.Services
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(s => s.Name == name, ct);

        if (exists)
            return AdminErrors.Services.AlreadyExists;

        var entity = new DomainService(
            id: Guid.CreateVersion7(),
            name: name,
            description: description);

        db.Services.Add(entity);

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
            0,
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