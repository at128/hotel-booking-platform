using System.Text;
using System.Text.Json;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Search;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Search.Queries.SearchHotels;

public sealed class SearchHotelsQueryHandler(IAppDbContext context)
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
        var query = context.Hotels
            .AsNoTracking()
            .Include(h => h.City)
            .Include(h => h.HotelServices).ThenInclude(hs => hs.Service)
            .AsQueryable();

        ApplyCityFilter();
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


        void ApplyCityFilter()
        {
            if (string.IsNullOrWhiteSpace(q.City))
                return;

            var city = Normalize(q.City);
            query = query.Where(h => h.City.Name.ToLower().Contains(city));
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
                var adults = q.Adults ?? DefaultAdults;
                var children = q.Children ?? DefaultChildren;
                var checkIn = q.CheckIn!.Value;
                var checkOut = q.CheckOut!.Value;
                var requiredRooms = Math.Max(DefaultRequiredRooms, q.NumberOfRooms ?? DefaultRequiredRooms);
                var now = DateTimeOffset.UtcNow;

                // Capacity + Availability must be satisfied by the SAME room type
                query = query.Where(h => h.HotelRoomTypes.Any(rt =>
                    (!hasOccupancyFilter || (rt.AdultCapacity >= adults && rt.ChildCapacity >= children)) &&
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

            // No dates provided => capacity-only filtering (if occupancy requested)
            if (!hasOccupancyFilter)
                return;

            var adultsOnly = q.Adults ?? DefaultAdults;
            var childrenOnly = q.Children ?? DefaultChildren;

            query = query.Where(h => h.HotelRoomTypes.Any(rt =>
                rt.AdultCapacity >= adultsOnly &&
                rt.ChildCapacity >= childrenOnly));
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