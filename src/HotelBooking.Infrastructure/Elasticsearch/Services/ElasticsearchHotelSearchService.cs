using System.Text;
using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Search;
using HotelBooking.Domain.Common.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotelBooking.Infrastructure.Elasticsearch.Services;

public sealed class ElasticsearchHotelSearchService : IHotelSearchService
{
    private readonly ElasticsearchClient _client;
    private readonly IEmbeddingService _embeddingService;
    private readonly ElasticsearchOptions _options;
    private readonly ILogger<ElasticsearchHotelSearchService> _logger;

    public ElasticsearchHotelSearchService(
        ElasticsearchClient client,
        IEmbeddingService embeddingService,
        IOptions<ElasticsearchOptions> options,
        ILogger<ElasticsearchHotelSearchService> logger)
    {
        _client = client;
        _embeddingService = embeddingService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<SearchHotelsResponse>> SearchAsync(
        HotelSearchRequest request,
        CancellationToken ct)
    {
        try
        {
            var limit = Math.Clamp(request.Limit, 1, 50);

            var searchText = !string.IsNullOrWhiteSpace(request.Query)
                ? request.Query
                : request.City;

            Query filterQuery = BuildFilterQuery(request);
            var sortOptions = BuildSortOptions(request.SortBy);

            ICollection<FieldValue>? searchAfter = null;
            if (!string.IsNullOrWhiteSpace(request.Cursor))
            {
                searchAfter = DecodeCursor(request.Cursor);
            }

            SearchResponse<HotelSearchDocument> response;

            if (_options.EnableSemanticSearch &&
                !string.IsNullOrWhiteSpace(searchText) &&
                await _embeddingService.IsAvailableAsync(ct))
            {
                var embedding = await _embeddingService.GenerateEmbeddingAsync(searchText, ct);

                if (embedding is not null)
                {
                    response = await ExecuteHybridSearchAsync(
                        searchText,
                        embedding,
                        filterQuery,
                        sortOptions,
                        searchAfter,
                        limit,
                        ct);
                }
                else
                {
                    response = await ExecuteTextSearchAsync(
                        searchText,
                        filterQuery,
                        sortOptions,
                        searchAfter,
                        limit,
                        ct);
                }
            }
            else
            {
                response = await ExecuteTextSearchAsync(
                    searchText,
                    filterQuery,
                    sortOptions,
                    searchAfter,
                    limit,
                    ct);
            }

            if (!response.IsValidResponse)
            {
                _logger.LogError("Elasticsearch search failed: {Reason}",
                    response.ElasticsearchServerError?.Error?.Reason ?? "Unknown error");

                return Error.Failure("Search.Failed", "Search service temporarily unavailable.");
            }

            var hits = response.Hits.ToList();
            var hasMore = hits.Count > limit;
            var pageHits = hits.Take(limit).ToList();

            var items = pageHits
                .Where(h => h.Source is not null)
                .Select(hit => new SearchHotelDto(
                    hit.Source!.Id,
                    hit.Source.Name,
                    hit.Source.StarRating,
                    hit.Source.Description,
                    hit.Source.CityName,
                    hit.Source.Country,
                    hit.Source.AverageRating,
                    hit.Source.ReviewCount,
                    hit.Source.ThumbnailUrl,
                    hit.Source.MinPricePerNight ?? 0,
                    hit.Source.Amenities
                ))
                .ToList();

            string? nextCursor = null;
            if (hasMore && pageHits.Count > 0)
            {
                var lastHit = pageHits[^1];
                if (lastHit.Sort is { Count: > 0 })
                {
                    nextCursor = EncodeCursor(lastHit.Sort);
                }
            }

            return new SearchHotelsResponse(items, nextCursor, hasMore, limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Elasticsearch search failed with exception");
            return Error.Failure("Search.Error", "An error occurred while searching.");
        }
    }

    private async Task<SearchResponse<HotelSearchDocument>> ExecuteTextSearchAsync(
        string? searchText,
        Query filterQuery,
        ICollection<SortOptions> sortOptions,
        ICollection<FieldValue>? searchAfter,
        int limit,
        CancellationToken ct)
    {
        Query finalQuery;

        if (string.IsNullOrWhiteSpace(searchText))
        {
            finalQuery = filterQuery;
        }
        else
        {
            Query textQuery = new MultiMatchQuery
            {
                Query = searchText,
                Fields = new[]
                {
                    "name^3",
                    "cityName^2",
                    "country^1.5",
                    "description",
                    "amenities",
                    "searchableText"
                },
                Fuzziness = new Fuzziness("AUTO"),
                PrefixLength = 1
            };

            finalQuery = new BoolQuery
            {
                Must = new List<Query> { textQuery },
                Filter = ExtractFilterClauses(filterQuery)
            };
        }

        var request = new SearchRequest(_options.HotelIndexName)
        {
            Query = finalQuery,
            Size = limit + 1,
            Sort = sortOptions,
            SearchAfter = searchAfter,
            Source = new SourceConfig(new SourceFilter
            {
                Excludes = new[] { "embedding", "searchableText" }
            })
        };

        return await _client.SearchAsync<HotelSearchDocument>(request, ct);
    }

    private async Task<SearchResponse<HotelSearchDocument>> ExecuteHybridSearchAsync(
        string searchText,
        float[] embedding,
        Query filterQuery,
        ICollection<SortOptions> sortOptions,
        ICollection<FieldValue>? searchAfter,
        int limit,
        CancellationToken ct)
    {
        Query textQuery = new MultiMatchQuery
        {
            Query = searchText,
            Fields = new[]
            {
                "name^3",
                "cityName^2",
                "country^1.5",
                "description",
                "amenities",
                "searchableText"
            },
            Fuzziness = new Fuzziness("AUTO"),
            PrefixLength = 1
        };

        Query baseQuery = new BoolQuery
        {
            Must = new List<Query> { textQuery },
            Filter = ExtractFilterClauses(filterQuery)
        };

        var request = new SearchRequest(_options.HotelIndexName)
        {
            Query = new ScriptScoreQuery
            {
                Query = baseQuery,
                Script = new Script
                {
                    Source = "cosineSimilarity(params.query_vector, 'embedding') + 1.0 + _score",
                    Params = new Dictionary<string, object>
                    {
                        ["query_vector"] = embedding
                    }
                }
            },
            Size = limit + 1,
            Sort = sortOptions,
            SearchAfter = searchAfter,
            Source = new SourceConfig(new SourceFilter
            {
                Excludes = new[] { "embedding", "searchableText" }
            })
        };

        return await _client.SearchAsync<HotelSearchDocument>(request, ct);
    }

    private static Query BuildFilterQuery(HotelSearchRequest request)
    {
        var filters = new List<Query>();

        if (request.MinStarRating.HasValue)
        {
            filters.Add(new NumberRangeQuery("starRating")
            {
                Gte = request.MinStarRating.Value
            });
        }

        if (request.MinPrice.HasValue)
        {
            filters.Add(new NumberRangeQuery("minPricePerNight")
            {
                Gte = (double)request.MinPrice.Value
            });
        }

        if (request.MaxPrice.HasValue)
        {
            filters.Add(new NumberRangeQuery("minPricePerNight")
            {
                Lte = (double)request.MaxPrice.Value
            });
        }

        if (request.RoomTypeId.HasValue)
        {
            filters.Add(new NestedQuery
            {
                Path = "roomTypes",
                Query = new TermQuery("roomTypes.roomTypeId")
                {
                    Value = request.RoomTypeId.Value.ToString()
                }
            });
        }

        if (request.Adults.HasValue || request.Children.HasValue)
        {
            var nestedMust = new List<Query>();

            if (request.Adults.HasValue)
            {
                nestedMust.Add(new NumberRangeQuery("roomTypes.adultCapacity")
                {
                    Gte = request.Adults.Value
                });
            }

            if (request.Children.HasValue)
            {
                nestedMust.Add(new NumberRangeQuery("roomTypes.childCapacity")
                {
                    Gte = request.Children.Value
                });
            }

            if (nestedMust.Count > 0)
            {
                filters.Add(new NestedQuery
                {
                    Path = "roomTypes",
                    Query = new BoolQuery
                    {
                        Must = nestedMust
                    }
                });
            }
        }

        if (request.Amenities is { Count: > 0 })
        {
            foreach (var amenity in request.Amenities.Where(a => !string.IsNullOrWhiteSpace(a)))
            {
                filters.Add(new TermQuery("amenities")
                {
                    Value = amenity.Trim()
                });
            }
        }

        if (filters.Count == 0)
        {
            return new MatchAllQuery();
        }

        return new BoolQuery
        {
            Filter = filters
        };
    }

    private static ICollection<Query>? ExtractFilterClauses(Query filterQuery)
    {
        if (filterQuery.TryGet<BoolQuery>(out var boolQuery))
        {
            return boolQuery.Filter;
        }

        if (filterQuery.TryGet<MatchAllQuery>(out _))
        {
            return null;
        }

        return new List<Query> { filterQuery };
    }

    private static ICollection<SortOptions> BuildSortOptions(string? sortBy)
    {
        var sortMode = sortBy?.Trim().ToLowerInvariant() switch
        {
            "price_asc" => ("minPricePerNight", SortOrder.Asc),
            "price_desc" => ("minPricePerNight", SortOrder.Desc),
            "stars_desc" => ("starRating", SortOrder.Desc),
            "rating_desc" => ("averageRating", SortOrder.Desc),
            _ => ("averageRating", SortOrder.Desc)
        };

        return new List<SortOptions>
        {
            SortOptions.Field(new Field(sortMode.Item1), new FieldSort { Order = sortMode.Item2 }),
            SortOptions.Field(new Field("id"), new FieldSort { Order = SortOrder.Asc })
        };
    }

    private static string EncodeCursor(IReadOnlyCollection<FieldValue> sortValues)
    {
        var values = sortValues.Select(GetFieldValueObject).ToArray();
        var json = JsonSerializer.Serialize(values);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private static ICollection<FieldValue>? DecodeCursor(string cursor)
    {
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var values = JsonSerializer.Deserialize<JsonElement[]>(json);

            if (values is null)
                return null;

            var result = new List<FieldValue>();

            foreach (var value in values)
            {
                result.Add(value.ValueKind switch
                {
                    JsonValueKind.String => FieldValue.String(value.GetString()!),
                    JsonValueKind.Number when value.TryGetInt64(out var l) => FieldValue.Long(l),
                    JsonValueKind.Number when value.TryGetDouble(out var d) => FieldValue.Double(d),
                    JsonValueKind.True => FieldValue.Boolean(true),
                    JsonValueKind.False => FieldValue.Boolean(false),
                    _ => FieldValue.Null
                });
            }

            return result;
        }
        catch
        {
            return null;
        }
    }

    private static object? GetFieldValueObject(FieldValue value)
    {
        if (value.TryGetString(out var s)) return s;
        if (value.TryGetLong(out var l)) return l;
        if (value.TryGetDouble(out var d)) return d;
        if (value.TryGetBool(out var b)) return b;
        return null;
    }

    public async Task IndexHotelAsync(HotelSearchDocument document, CancellationToken ct)
    {
        var response = await _client.IndexAsync(
            document,
            idx => idx.Index(_options.HotelIndexName).Id(document.Id.ToString()),
            ct);

        if (!response.IsValidResponse)
        {
            _logger.LogError("Failed to index hotel {HotelId}: {Error}",
                document.Id, response.ElasticsearchServerError?.Error?.Reason);
        }
    }

    public async Task BulkIndexAsync(IReadOnlyCollection<HotelSearchDocument> documents, CancellationToken ct)
    {
        if (documents.Count == 0) return;

        var response = await _client.BulkAsync(b =>
        {
            b.Index(_options.HotelIndexName);

            foreach (var doc in documents)
            {
                b.Index(doc, i => i.Id(doc.Id.ToString()));
            }
        }, ct);

        if (response.Errors)
        {
            var errorCount = response.ItemsWithErrors.Count();
            _logger.LogError("Bulk indexing had {ErrorCount} errors out of {Total}",
                errorCount, documents.Count);
        }
        else
        {
            _logger.LogInformation("Successfully indexed {Count} hotels", documents.Count);
        }
    }

    public async Task RemoveHotelAsync(Guid hotelId, CancellationToken ct)
    {
        await _client.DeleteAsync<HotelSearchDocument>(
            hotelId.ToString(),
            d => d.Index(_options.HotelIndexName),
            ct);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct)
    {
        try
        {
            var response = await _client.PingAsync(ct);
            return response.IsValidResponse;
        }
        catch
        {
            return false;
        }
    }
}