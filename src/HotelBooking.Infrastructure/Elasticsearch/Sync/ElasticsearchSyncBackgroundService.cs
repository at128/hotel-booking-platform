using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Infrastructure.Elasticsearch.Initialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotelBooking.Infrastructure.Elasticsearch.Sync;

public sealed class ElasticsearchSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ElasticsearchOptions _options;
    private readonly ILogger<ElasticsearchSyncBackgroundService> _logger;
    private DateTimeOffset _lastSyncTime = DateTimeOffset.MinValue;

    public ElasticsearchSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<ElasticsearchOptions> options,
        ILogger<ElasticsearchSyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        await InitializeIndexAsync(stoppingToken);
        await FullSyncAsync(stoppingToken);

        var interval = TimeSpan.FromMinutes(_options.SyncIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(interval, stoppingToken);

            try
            {
                await IncrementalSyncAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during incremental sync");
            }
        }
    }

    private async Task InitializeIndexAsync(CancellationToken ct)
    {
        const int maxRetries = 10;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var initializer = scope.ServiceProvider
                    .GetRequiredService<ElasticsearchIndexInitializer>();

                await initializer.InitializeAsync(ct);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to initialize ES index (attempt {Attempt}/{Max}), retrying in 10s...",
                    attempt,
                    maxRetries);

                await Task.Delay(TimeSpan.FromSeconds(10), ct);
            }
        }

        _logger.LogError("Failed to initialize Elasticsearch index after {Max} attempts", maxRetries);
    }

    private async Task FullSyncAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting full Elasticsearch sync...");
        var syncTime = DateTimeOffset.UtcNow;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var searchService = scope.ServiceProvider.GetRequiredService<IHotelSearchService>();
            var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

            var hotels = await db.Hotels
                .AsNoTracking()
                .AsSplitQuery()
                .Include(h => h.City)
                .Include(h => h.HotelServices)
                    .ThenInclude(hs => hs.Service)
                .Include(h => h.HotelRoomTypes.Where(rt => rt.DeletedAtUtc == null))
                    .ThenInclude(rt => rt.RoomType)
                .Include(h => h.HotelRoomTypes.Where(rt => rt.DeletedAtUtc == null))
                    .ThenInclude(rt => rt.Rooms.Where(r => r.DeletedAtUtc == null))
                .Where(h => h.DeletedAtUtc == null)
                .ToListAsync(ct);

            _logger.LogInformation("Found {Count} hotels to index", hotels.Count);

            var documents = hotels.Select(BuildDocument).ToList();

            if (_options.EnableSemanticSearch && await embeddingService.IsAvailableAsync(ct))
            {
                await GenerateEmbeddingsForDocumentsAsync(documents, embeddingService, ct);
            }

            const int batchSize = 100;
            for (var i = 0; i < documents.Count; i += batchSize)
            {
                var batch = documents.Skip(i).Take(batchSize).ToList();
                await searchService.BulkIndexAsync(batch, ct);
            }

            _lastSyncTime = syncTime;
            _logger.LogInformation("Full sync completed: {Count} hotels indexed", documents.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Full sync failed");
        }
    }

    private async Task IncrementalSyncAsync(CancellationToken ct)
    {
        var syncTime = DateTimeOffset.UtcNow;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var searchService = scope.ServiceProvider.GetRequiredService<IHotelSearchService>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

        var modifiedHotels = await db.Hotels
            .AsNoTracking()
            .AsSplitQuery()
            .Include(h => h.City)
            .Include(h => h.HotelServices)
                .ThenInclude(hs => hs.Service)
            .Include(h => h.HotelRoomTypes.Where(rt => rt.DeletedAtUtc == null))
                .ThenInclude(rt => rt.RoomType)
            .Include(h => h.HotelRoomTypes.Where(rt => rt.DeletedAtUtc == null))
                .ThenInclude(rt => rt.Rooms.Where(r => r.DeletedAtUtc == null))
            .Where(h => h.LastModifiedUtc >= _lastSyncTime)
            .ToListAsync(ct);

        if (modifiedHotels.Count == 0)
            return;

        _logger.LogInformation("Incremental sync: {Count} modified hotels", modifiedHotels.Count);

        var activeHotels = modifiedHotels.Where(h => h.DeletedAtUtc == null).ToList();
        var deletedHotels = modifiedHotels.Where(h => h.DeletedAtUtc != null).ToList();

        foreach (var hotel in deletedHotels)
        {
            await searchService.RemoveHotelAsync(hotel.Id, ct);
        }

        if (activeHotels.Count > 0)
        {
            var documents = activeHotels.Select(BuildDocument).ToList();

            if (_options.EnableSemanticSearch && await embeddingService.IsAvailableAsync(ct))
            {
                await GenerateEmbeddingsForDocumentsAsync(documents, embeddingService, ct);
            }

            await searchService.BulkIndexAsync(documents, ct);
        }

        _lastSyncTime = syncTime;
        _logger.LogInformation(
            "Incremental sync completed: {Indexed} indexed, {Removed} removed",
            activeHotels.Count,
            deletedHotels.Count);
    }

    private static HotelSearchDocument BuildDocument(Domain.Hotels.Hotel hotel)
    {
        var amenities = hotel.HotelServices
            .Select(hs => hs.Service.Name)
            .Distinct()
            .ToList();

        var roomTypes = hotel.HotelRoomTypes
            .Select(rt => new RoomTypeInfo(
                rt.RoomTypeId,
                rt.RoomType.Name,
                rt.PricePerNight,
                rt.AdultCapacity,
                rt.ChildCapacity,
                rt.Rooms.Count(r => r.DeletedAtUtc == null)))
            .ToList();

        var searchableText = string.Join(" ",
            new[] { hotel.Name, hotel.City.Name, hotel.City.Country, hotel.Description }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Concat(amenities));

        return new HotelSearchDocument
        {
            Id = hotel.Id,
            Name = hotel.Name,
            CityName = hotel.City.Name,
            Country = hotel.City.Country,
            CityId = hotel.CityId,
            Description = hotel.Description,
            Owner = hotel.Owner,
            StarRating = hotel.StarRating,
            MinPricePerNight = hotel.MinPricePerNight,
            AverageRating = hotel.AverageRating,
            ReviewCount = hotel.ReviewCount,
            ThumbnailUrl = hotel.ThumbnailUrl,
            Amenities = amenities,
            RoomTypes = roomTypes,
            SearchableText = searchableText
        };
    }

    private async Task GenerateEmbeddingsForDocumentsAsync(
        List<HotelSearchDocument> documents,
        IEmbeddingService embeddingService,
        CancellationToken ct)
    {
        const int embeddingBatchSize = 32;

        for (var i = 0; i < documents.Count; i += embeddingBatchSize)
        {
            var batch = documents.Skip(i).Take(embeddingBatchSize).ToList();
            var texts = batch
                .Select(d => d.SearchableText ?? d.Name)
                .ToList();

            try
            {
                var embeddings = await embeddingService.GenerateEmbeddingsAsync(texts, ct);

                for (var j = 0; j < batch.Count && j < embeddings.Count; j++)
                {
                    if (embeddings[j] is not null)
                    {
                        documents[i + j] = documents[i + j] with { Embedding = embeddings[j] };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to generate embeddings for batch starting at {Index}", i);
            }
        }
    }
}