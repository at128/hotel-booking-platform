using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Infrastructure.Elasticsearch.Initialization;
using HotelBooking.Infrastructure.Elasticsearch.Services;
using HotelBooking.Infrastructure.Elasticsearch.Sync;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Infrastructure.Elasticsearch;

public static class ElasticsearchServiceExtensions
{
    public static IServiceCollection AddElasticsearch(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ElasticsearchOptions>(
            configuration.GetSection(ElasticsearchOptions.SectionName));

        services.Configure<EmbeddingOptions>(
            configuration.GetSection(EmbeddingOptions.SectionName));

        services.AddSingleton(sp =>
        {
            var options = configuration
                .GetSection(ElasticsearchOptions.SectionName)
                .Get<ElasticsearchOptions>() ?? new ElasticsearchOptions();

            var settings = new ElasticsearchClientSettings(new Uri(options.Url))
                .Authentication(new BasicAuthentication(options.Username, options.Password))
                .DefaultIndex(options.HotelIndexName)
                .RequestTimeout(TimeSpan.FromSeconds(30));

            if (options.AllowInvalidCertificates)
            {
                settings.ServerCertificateValidationCallback((_, _, _, _) => true);
            }

            return new ElasticsearchClient(settings);
        });

        services.AddHttpClient<IEmbeddingService, HttpEmbeddingService>((_, client) =>
        {
            var options = configuration
                .GetSection(EmbeddingOptions.SectionName)
                .Get<EmbeddingOptions>() ?? new EmbeddingOptions();

            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
            }
        });

        services.AddScoped<IHotelSearchService, ElasticsearchHotelSearchService>();
        services.AddScoped<ElasticsearchIndexInitializer>();
        services.AddHostedService<ElasticsearchSyncBackgroundService>();

        return services;
    }
}