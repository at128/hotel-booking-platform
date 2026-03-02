using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Settings;
using HotelBooking.Contracts.Home;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HotelBooking.Application.Features.Home.Queries.GetSearchConfig;

public sealed class GetSearchConfigQueryHandler(
    IAppDbContext context,
    IOptions<BookingSettings> settings)
    : IRequestHandler<GetSearchConfigQuery, Result<SearchConfigResponse>>
{
    public async Task<Result<SearchConfigResponse>> Handle(
        GetSearchConfigQuery request, CancellationToken ct)
    {
        var amenities = await context.Services
            .Select(s => s.Name)
            .OrderBy(n => n)
            .ToListAsync(ct);

        var cfg = settings.Value;

        return new SearchConfigResponse(
            DefaultAdults: 2,
            DefaultChildren: 0,
            DefaultRooms: 1,
            MaxRooms: 9,
            MaxAdvanceBookingDays: cfg.MaxAdvanceBookingDays,
            TaxRate: cfg.TaxRate,
            AvailableAmenities: amenities);
    }
}