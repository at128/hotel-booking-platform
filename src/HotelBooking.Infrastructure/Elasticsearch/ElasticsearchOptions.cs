namespace HotelBooking.Infrastructure.Elasticsearch;

public sealed class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";

    public string Url { get; set; } = "http://elasticsearch:9200";

    public string Username { get; set; } = "elastic";

    public string Password { get; set; } = string.Empty;

    public string HotelIndexName { get; set; } = "hotels";

    public bool AllowInvalidCertificates { get; set; } = true;

    public bool EnableSemanticSearch { get; set; } = true;

    public int SyncIntervalMinutes { get; set; } = 5;

    public int NumberOfShards { get; set; } = 1;

    public int NumberOfReplicas { get; set; } = 0;
}