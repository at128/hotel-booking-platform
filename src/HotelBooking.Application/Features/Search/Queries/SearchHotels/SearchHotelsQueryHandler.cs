using System.Text;
using System.Text.Json;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Search;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Features.Search.Queries.SearchHotels;

public sealed class SearchHotelsQueryHandler(
    IAppDbContext context,
    IHotelSearchService searchService,
    ILogger<SearchHotelsQueryHandler> logger)
    : IRequestHandler<SearchHotelsQuery, Result<SearchHotelsResponse>>
{
    private const int DefaultAdults = 2;
    private const int DefaultChildren = 0;
    private const int DefaultRequiredRooms = 1;
    private const int MinLimit = 1;
    private const int MaxLimit = 50;

    private sealed record CursorData(Guid Id, decimal Value);

    private enum SortMode
    {
        RatingDesc = 0,
        PriceAsc = 1,
        PriceDesc = 2,
        StarsDesc = 3
    }

    public async Task<Result<SearchHotelsResponse>> Handle(SearchHotelsQuery q, CancellationToken ct)
    {
        // ── Try Elasticsearch first ──
        if (await TryElasticsearchAsync(q, ct) is { } esResult)
        {
            return esResult;
        }

        // ── Fallback to SQL Server ──
        logger.LogWarning("Elasticsearch unavailable, falling back to SQL Server query");
        return await ExecuteSqlFallbackAsync(q, ct);
    }

    // ─────────────────────────────────────────────────────────────────
    // Elasticsearch path
    // ─────────────────────────────────────────────────────────────────

    private async Task<Result<SearchHotelsResponse>?> TryElasticsearchAsync(
        SearchHotelsQuery q, CancellationToken ct)
    {
        try
        {
            if (!await searchService.IsAvailableAsync(ct))
                return null;

            var request = new HotelSearchRequest(
                Query: q.Query,
                City: q.City,
                RoomTypeId: q.RoomTypeId,
                CheckIn: q.CheckIn,
                CheckOut: q.CheckOut,
                Adults: q.Adults,
                Children: q.Children,
                NumberOfRooms: q.NumberOfRooms,
                MinPrice: q.MinPrice,
                MaxPrice: q.MaxPrice,
                MinStarRating: q.MinStarRating,
                Amenities: q.Amenities,
                SortBy: q.SortBy,
                Cursor: q.Cursor,
                Limit: q.Limit);

            var result = await searchService.SearchAsync(request, ct);

            // If ES returned a failure, fall back to SQL
            if (!result.IsSuccess)
            {
                logger.LogWarning("Elasticsearch search failed, falling back to SQL Server");
                return null;
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Elasticsearch call threw exception, falling back to SQL Server");
            return null;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // SQL Server fallback (original implementation preserved)
    // ─────────────────────────────────────────────────────────────────

    private async Task<Result<SearchHotelsResponse>> ExecuteSqlFallbackAsync(
        SearchHotelsQuery q, CancellationToken ct)
    {
        var effectiveCheckIn = q.CheckIn ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var effectiveCheckOut = q.CheckOut ?? effectiveCheckIn.AddDays(1);
        var effectiveAdults = q.Adults ?? DefaultAdults;
        var effectiveChildren = q.Children ?? DefaultChildren;
        var effectiveRooms = Math.Max(DefaultRequiredRooms, q.NumberOfRooms ?? DefaultRequiredRooms);

        var query = context.Hotels
            .AsNoTracking()
            .AsSplitQuery()
            .Include(h => h.City)
            .Include(h => h.HotelServices)
                .ThenInclude(hs => hs.Service)
            .AsQueryable();

        ApplyTextSearchFilter();
        ApplyRoomTypeFilter();
        ApplyStarRatingFilter();
        ApplyPriceRangeFilter();
        ApplyOccupancyAndAvailabilityFilter();
        ApplyAmenitiesFilter();

        var sortMode = ParseSortMode(q.SortBy);
        ApplySorting(sortMode);
        ApplyCursorFilter(sortMode);

        var limit = Math.Clamp(q.Limit, MinLimit, MaxLimit);
        var hotels = await query.Take(limit + 1).ToListAsync(ct);

        var hasMore = hotels.Count > limit;
        var page = hotels.Take(limit).ToList();

        var items = page.Select(h => new SearchHotelDto(
            h.Id,
            h.Name,
            h.StarRating,
            h.Description,
            h.City.Name,
            h.City.Country,
            h.AverageRating,
            h.ReviewCount,
            h.ThumbnailUrl,
            h.MinPricePerNight ?? 0,
            h.HotelServices.Select(x => x.Service.Name).ToList()
        )).ToList();

        string? nextCursor = null;
        if (hasMore && page.Count > 0)
        {
            var last = page[^1];
            var cursorValue = sortMode switch
            {
                SortMode.PriceAsc or SortMode.PriceDesc => (last.MinPricePerNight ?? 0),
                SortMode.StarsDesc => last.StarRating,
                _ => (decimal)last.AverageRating
            };

            nextCursor = EncodeCursor(last.Id, cursorValue);
        }

        return new SearchHotelsResponse(items, nextCursor, hasMore, limit);

        // ── Local functions (same as original) ──

        void ApplyTextSearchFilter()
        {
            var searchText = !string.IsNullOrWhiteSpace(q.Query)
                ? q.Query
                : q.City;

            if (string.IsNullOrWhiteSpace(searchText))
                return;

            var term = Normalize(searchText);

            query = query.Where(h =>
                h.Name.ToLower().Contains(term) ||
                h.City.Name.ToLower().Contains(term));
        }

        void ApplyRoomTypeFilter()
        {
            if (!q.RoomTypeId.HasValue)
                return;

            query = query.Where(h =>
                h.HotelRoomTypes.Any(rt => rt.RoomTypeId == q.RoomTypeId.Value));
        }

        void ApplyStarRatingFilter()
        {
            if (!q.MinStarRating.HasValue)
                return;

            query = query.Where(h => h.StarRating >= q.MinStarRating.Value);
        }

        void ApplyPriceRangeFilter()
        {
            if (q.MinPrice.HasValue)
                query = query.Where(h => (h.MinPricePerNight ?? 0) >= q.MinPrice.Value);

            if (q.MaxPrice.HasValue)
                query = query.Where(h => (h.MinPricePerNight ?? 0) <= q.MaxPrice.Value);
        }

        void ApplyOccupancyAndAvailabilityFilter()
        {
            var hasOccupancyFilter = q.Adults.HasValue || q.Children.HasValue;
            var hasAvailabilityFilter = q.CheckIn.HasValue && q.CheckOut.HasValue;

            if (hasAvailabilityFilter)
            {
                var checkIn = effectiveCheckIn;
                var checkOut = effectiveCheckOut;
                var requiredRooms = effectiveRooms;
                var now = DateTimeOffset.UtcNow;

                query = query.Where(h => h.HotelRoomTypes.Any(rt =>
                    (!q.RoomTypeId.HasValue || rt.RoomTypeId == q.RoomTypeId.Value) &&
                    (!hasOccupancyFilter || (rt.AdultCapacity >= effectiveAdults && rt.ChildCapacity >= effectiveChildren)) &&
                    (
                        rt.Rooms.Count(r => r.DeletedAtUtc == null)

                        - context.BookingRooms.Count(br =>
                            br.HotelRoomTypeId == rt.Id &&
                            br.Booking.Status != BookingStatus.Cancelled &&
                            br.Booking.Status != BookingStatus.Failed &&
                            br.Booking.CheckIn < checkOut &&
                            br.Booking.CheckOut > checkIn)

                        - (context.CheckoutHolds
                            .Where(ch =>
                                ch.HotelRoomTypeId == rt.Id &&
                                !ch.IsReleased &&
                                ch.ExpiresAtUtc > now &&
                                ch.CheckIn < checkOut &&
                                ch.CheckOut > checkIn)
                            .Sum(ch => (int?)ch.Quantity) ?? 0)
                    ) >= requiredRooms
                ));

                return;
            }

            if (!hasOccupancyFilter)
                return;

            query = query.Where(h => h.HotelRoomTypes.Any(rt =>
                (!q.RoomTypeId.HasValue || rt.RoomTypeId == q.RoomTypeId.Value) &&
                rt.AdultCapacity >= effectiveAdults &&
                rt.ChildCapacity >= effectiveChildren));
        }

        void ApplyAmenitiesFilter()
        {
            if (q.Amenities is not { Count: > 0 })
                return;

            var normalizedAmenities = q.Amenities
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(Normalize)
                .Distinct()
                .ToList();

            if (normalizedAmenities.Count == 0)
                return;

            foreach (var amenity in normalizedAmenities)
            {
                query = query.Where(h =>
                    h.HotelServices.Any(hs => hs.Service.Name.ToLower() == amenity));
            }
        }

        void ApplySorting(SortMode sortMode)
        {
            query = sortMode switch
            {
                SortMode.PriceAsc => query.OrderBy(h => h.MinPricePerNight ?? 0).ThenBy(h => h.Id),
                SortMode.PriceDesc => query.OrderByDescending(h => h.MinPricePerNight ?? 0).ThenBy(h => h.Id),
                SortMode.StarsDesc => query.OrderByDescending(h => h.StarRating).ThenBy(h => h.Id),
                _ => query.OrderByDescending(h => h.AverageRating).ThenBy(h => h.Id)
            };
        }

        void ApplyCursorFilter(SortMode sortMode)
        {
            if (string.IsNullOrWhiteSpace(q.Cursor))
                return;

            var cursor = DecodeCursor(q.Cursor);
            if (cursor is null)
                return;

            query = sortMode switch
            {
                SortMode.PriceAsc => query.Where(h =>
                    (h.MinPricePerNight ?? 0) > cursor.Value ||
                    ((h.MinPricePerNight ?? 0) == cursor.Value && h.Id.CompareTo(cursor.Id) > 0)),

                SortMode.PriceDesc => query.Where(h =>
                    (h.MinPricePerNight ?? 0) < cursor.Value ||
                    ((h.MinPricePerNight ?? 0) == cursor.Value && h.Id.CompareTo(cursor.Id) > 0)),

                SortMode.StarsDesc => query.Where(h =>
                    h.StarRating < (short)cursor.Value ||
                    (h.StarRating == (short)cursor.Value && h.Id.CompareTo(cursor.Id) > 0)),

                _ => query.Where(h =>
                    h.AverageRating < (double)cursor.Value ||
                    (h.AverageRating == (double)cursor.Value && h.Id.CompareTo(cursor.Id) > 0))
            };
        }
    }

    private static SortMode ParseSortMode(string? sortBy)
    {
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "price_asc" => SortMode.PriceAsc,
            "price_desc" => SortMode.PriceDesc,
            "stars_desc" => SortMode.StarsDesc,
            "rating_desc" => SortMode.RatingDesc,
            _ => SortMode.RatingDesc
        };
    }

    private static string Normalize(string value)
        => value.Trim().ToLower();

    private static string EncodeCursor(Guid id, decimal value)
    {
        var json = JsonSerializer.Serialize(new { id, value });
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private static CursorData? DecodeCursor(string cursor)
    {
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            using var doc = JsonDocument.Parse(json);

            var id = doc.RootElement.GetProperty("id").GetGuid();
            var value = doc.RootElement.GetProperty("value").GetDecimal();

            return new CursorData(id, value);
        }
        catch
        {
            return null;
        }
    }
}