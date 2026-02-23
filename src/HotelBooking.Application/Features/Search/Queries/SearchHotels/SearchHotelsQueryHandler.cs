using System.Text;
using System.Text.Json;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Search;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Search.Queries.SearchHotels;

public sealed class SearchHotelsQueryHandler(IAppDbContext context)
    : IRequestHandler<SearchHotelsQuery, Result<SearchHotelsResponse>>
{
    private sealed record CursorData(Guid Id, decimal Value);

    public async Task<Result<SearchHotelsResponse>> Handle(SearchHotelsQuery q, CancellationToken ct)
    {
        var query = context.Hotels
            .Include(h => h.City)
            .Include(h => h.HotelServices).ThenInclude(hs => hs.Service)
            .Include(h => h.HotelRoomTypes)
            .AsQueryable();


        if (!string.IsNullOrWhiteSpace(q.City))
        {
            var city = q.City.Trim().ToLower();
            query = query.Where(h => h.City.Name.ToLower().Contains(city));
        }

        if (q.MinStarRating.HasValue)
            query = query.Where(h => h.StarRating >= q.MinStarRating.Value);

        if (q.MinPrice.HasValue)
            query = query.Where(h => (h.MinPricePerNight ?? 0) >= q.MinPrice.Value);

        if (q.MaxPrice.HasValue)
            query = query.Where(h => (h.MinPricePerNight ?? 0) <= q.MaxPrice.Value);

        if (q.Adults.HasValue || q.Children.HasValue)
        {
            var adults = q.Adults ?? 2;
            var children = q.Children ?? 0;

            query = query.Where(h => h.HotelRoomTypes.Any(rt =>
                rt.AdultCapacity >= adults && rt.ChildCapacity >= children));
        }

        if (q.Amenities is { Count: > 0 })
        {
            foreach (var amenity in q.Amenities)
            {
                if (string.IsNullOrWhiteSpace(amenity)) continue;
                var a = amenity.Trim().ToLower();

                query = query.Where(h =>
                    h.HotelServices.Any(hs => hs.Service.Name.ToLower() == a));
            }
        }


        query = q.SortBy switch
        {
            "price_asc" => query.OrderBy(h => (h.MinPricePerNight ?? 0)).ThenBy(h => h.Id),
            "price_desc" => query.OrderByDescending(h => (h.MinPricePerNight ?? 0)).ThenBy(h => h.Id),
            "rating_desc" => query.OrderByDescending(h => h.AverageRating).ThenBy(h => h.Id),
            "stars_desc" => query.OrderByDescending(h => h.StarRating).ThenBy(h => h.Id),
            _ => query.OrderByDescending(h => h.AverageRating).ThenBy(h => h.Id)
        };


        if (!string.IsNullOrWhiteSpace(q.Cursor))
        {
            var cursor = DecodeCursor(q.Cursor);
            if (cursor is not null)
            {
                query = q.SortBy switch
                {
                    "price_asc" => query.Where(h =>
                        (h.MinPricePerNight ?? 0) > cursor.Value ||
                        ((h.MinPricePerNight ?? 0) == cursor.Value && h.Id.CompareTo(cursor.Id) > 0)),

                    "price_desc" => query.Where(h =>
                        (h.MinPricePerNight ?? 0) < cursor.Value ||
                        ((h.MinPricePerNight ?? 0) == cursor.Value && h.Id.CompareTo(cursor.Id) > 0)),

                    "rating_desc" => query.Where(h =>
                        h.AverageRating < (double)cursor.Value ||
                        (h.AverageRating == (double)cursor.Value && h.Id.CompareTo(cursor.Id) > 0)),

                    "stars_desc" => query.Where(h =>
                        h.StarRating < (short)cursor.Value ||
                        (h.StarRating == (short)cursor.Value && h.Id.CompareTo(cursor.Id) > 0)),

                    _ => query.Where(h =>
                        h.AverageRating < (double)cursor.Value ||
                        (h.AverageRating == (double)cursor.Value && h.Id.CompareTo(cursor.Id) > 0))
                };
            }
        }

        var limit = Math.Clamp(q.Limit, 1, 50);
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
            (h.MinPricePerNight ?? 0),
            h.HotelServices.Select(x => x.Service.Name).ToList()
        )).ToList();

        string? nextCursor = null;
        if (hasMore && page.Count > 0)
        {
            var last = page[^1];
            var cursorValue = q.SortBy switch
            {
                "price_asc" or "price_desc" => (last.MinPricePerNight ?? 0),
                "rating_desc" => (decimal)last.AverageRating,
                "stars_desc" => last.StarRating,
                _ => (decimal)last.AverageRating
            };

            nextCursor = EncodeCursor(last.Id, cursorValue);
        }

        return new SearchHotelsResponse(items, nextCursor, hasMore, limit);
    }

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