using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Services.Queries.GetServiceById;

public sealed class GetServiceByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetServiceByIdQuery, Result<ServiceDto>>
{
    public async Task<Result<ServiceDto>> Handle(GetServiceByIdQuery q, CancellationToken ct)
    {
        var item = await db.Services
            .AsNoTracking()
            .Where(s => s.Id == q.Id && s.DeletedAtUtc == null)
            .Select(s => new ServiceDto(
                s.Id,
                s.Name,
                s.Description,
                s.HotelServices.Count(),
                s.CreatedAtUtc,
                s.UpdatedAtUtc))
            .FirstOrDefaultAsync(ct);

        if (item is null)
            return AdminErrors.Services.NotFound(q.Id);

        return item;
    }
}