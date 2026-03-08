using System.Net.Http.Json;
using System.Text.Json.Serialization;
using HotelBooking.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotelBooking.Infrastructure.Elasticsearch.Services;

public sealed class HttpEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly EmbeddingOptions _options;
    private readonly ILogger<HttpEmbeddingService> _logger;

    public HttpEmbeddingService(
        HttpClient httpClient,
        IOptions<EmbeddingOptions> options,
        ILogger<HttpEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public int Dimensions => _options.Dimensions;

    public async Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken ct)
    {
        var results = await GenerateEmbeddingsAsync([text], ct);
        return results.Count > 0 ? results[0] : null;
    }

    public async Task<IReadOnlyList<float[]?>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct)
    {
        if (texts.Count == 0) return [];

        try
        {
            var request = new EmbeddingRequest
            {
                Input = texts.ToList(),
                Model = _options.Model
            };

            var response = await _httpClient.PostAsJsonAsync(
                _options.EmbeddingsEndpoint,
                request,
                ct);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: ct);

            if (result?.Data is null)
                return texts.Select(_ => (float[]?)null).ToList();

            return result.Data
                .OrderBy(d => d.Index)
                .Select(d => (float[]?)d.Embedding)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Embedding service call failed for {Count} texts", texts.Count);
            return texts.Select(_ => (float[]?)null).ToList();
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private sealed class EmbeddingRequest
    {
        [JsonPropertyName("input")]
        public List<string> Input { get; set; } = [];

        [JsonPropertyName("model")]
        public string? Model { get; set; }
    }

    private sealed class EmbeddingResponse
    {
        [JsonPropertyName("data")]
        public List<EmbeddingData>? Data { get; set; }
    }

    private sealed class EmbeddingData
    {
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = [];

        [JsonPropertyName("index")]
        public int Index { get; set; }
    }
}

public sealed class EmbeddingOptions
{
    public const string SectionName = "Embedding";

    public string BaseUrl { get; set; } = "http://embedding-service:8000";

    public string EmbeddingsEndpoint { get; set; } = "/embeddings";

    public string Model { get; set; } = "all-MiniLM-L6-v2";

    public int Dimensions { get; set; } = 384;

    public string? ApiKey { get; set; }

    public int TimeoutSeconds { get; set; } = 30;
}