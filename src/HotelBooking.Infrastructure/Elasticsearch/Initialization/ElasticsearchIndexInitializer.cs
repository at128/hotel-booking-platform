using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using HotelBooking.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotelBooking.Infrastructure.Elasticsearch.Initialization;

public sealed class ElasticsearchIndexInitializer
{
    private readonly ElasticsearchClient _client;
    private readonly ElasticsearchOptions _options;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<ElasticsearchIndexInitializer> _logger;

    public ElasticsearchIndexInitializer(
        ElasticsearchClient client,
        IOptions<ElasticsearchOptions> options,
        IEmbeddingService embeddingService,
        ILogger<ElasticsearchIndexInitializer> logger)
    {
        _client = client;
        _options = options.Value;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        var existsResponse = await _client.Indices.ExistsAsync(_options.HotelIndexName, ct);
        if (existsResponse.Exists)
        {
            _logger.LogInformation("Elasticsearch index '{IndexName}' already exists", _options.HotelIndexName);
            return;
        }

        var embeddingDimensions = _options.EnableSemanticSearch
            ? _embeddingService.Dimensions
            : 384;

        var properties = new Properties
        {
            ["id"] = new KeywordProperty(),
            ["name"] = new TextProperty
            {
                Fields = new Properties
                {
                    ["keyword"] = new KeywordProperty()
                }
            },
            ["cityName"] = new TextProperty
            {
                Fields = new Properties
                {
                    ["keyword"] = new KeywordProperty()
                }
            },
            ["country"] = new KeywordProperty(),
            ["cityId"] = new KeywordProperty(),
            ["description"] = new TextProperty(),
            ["owner"] = new KeywordProperty(),
            ["starRating"] = new ShortNumberProperty(),
            ["minPricePerNight"] = new DoubleNumberProperty(),
            ["averageRating"] = new DoubleNumberProperty(),
            ["reviewCount"] = new IntegerNumberProperty(),
            ["thumbnailUrl"] = new KeywordProperty(),
            ["amenities"] = new KeywordProperty(),
            ["searchableText"] = new TextProperty(),
            ["embedding"] = new DenseVectorProperty
            {
                Dims = embeddingDimensions,
                Index = true,
                Similarity = "cosine"
            },
            ["roomTypes"] = new NestedProperty
            {
                Properties = new Properties
                {
                    ["roomTypeId"] = new KeywordProperty(),
                    ["roomTypeName"] = new TextProperty(),
                    ["pricePerNight"] = new DoubleNumberProperty(),
                    ["adultCapacity"] = new ShortNumberProperty(),
                    ["childCapacity"] = new ShortNumberProperty(),
                    ["availableRoomCount"] = new IntegerNumberProperty()
                }
            }
        };

        var createResponse = await _client.Indices.CreateAsync(
            _options.HotelIndexName,
            idx => idx
                .Settings(s => s
                    .NumberOfShards(_options.NumberOfShards)
                    .NumberOfReplicas(_options.NumberOfReplicas))
                .Mappings(new TypeMapping
                {
                    Properties = properties
                }),
            ct);

        if (!createResponse.IsValidResponse)
        {
            _logger.LogError(
                "Failed to create Elasticsearch index '{IndexName}': {Error}",
                _options.HotelIndexName,
                createResponse.ElasticsearchServerError?.Error?.Reason);

            throw new InvalidOperationException(
                $"Failed to create Elasticsearch index '{_options.HotelIndexName}'.");
        }

        _logger.LogInformation(
            "Created Elasticsearch index '{IndexName}' (semantic search: {SemanticEnabled}, dims: {Dims})",
            _options.HotelIndexName,
            _options.EnableSemanticSearch,
            embeddingDimensions);
    }
}