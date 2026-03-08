using HotelBooking.Api.BackgroundJobs;
using HotelBooking.Api.Infrastructure;
using HotelBooking.Api.Services;
using HotelBooking.Api.Services.Images;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Infrastructure.Settings;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Threading.RateLimiting;
using ForwardedIpNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace HotelBooking.Api;

public static class DependencyInjection
{
    private const string FrontendCorsPolicy = "Frontend";
    private const string AuthRateLimitPolicy = "auth";
    private const string AdminUploadsRateLimitPolicy = "admin-uploads";
    private const string WebhooksRateLimitPolicy = "webhooks";

    public static IServiceCollection AddPresentation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPresentationCore();
        services.AddApiVersioningServices();
        services.AddSwaggerDocumentation();
        services.AddRateLimitingPolicies(configuration);
        services.AddForwardedHeadersSupport(configuration);
        services.AddCorsPolicy(configuration);

        services.AddScoped<ICookieService, CookieService>();

        services.AddCookieSettings(configuration);

        services.Configure<CookieSettings>(
            configuration.GetSection("CookieSettings"));

        services.AddExpirePendingPaymentsJobSettings(configuration);
        services.AddHotelImageUploadServices(configuration);

        return services;
    }

    public static WebApplication UseCoreMiddlewares(this WebApplication app)
    {
        app.UseDiagnosticsAndErrorHandling();
        app.UseSwaggerAndHsts();
        app.UseHttpSecurityPipeline();
        app.UseStaticFiles();

        return app;
    }

    private static IServiceCollection AddPresentationCore(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();

        services.AddHttpContextAccessor();
        services.AddScoped<IUser, CurrentUser>();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddMemoryCache();

        return services;
    }

    private static IServiceCollection AddApiVersioningServices(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new Asp.Versioning.UrlSegmentApiVersionReader();
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }

    private static IServiceCollection AddRateLimitingPolicies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var globalTokenLimit = GetPositiveIntOrDefault(
            configuration,
            "RateLimiting:Global:TokenLimit",
            300);
        var globalTokensPerPeriod = GetPositiveIntOrDefault(
            configuration,
            "RateLimiting:Global:TokensPerPeriod",
            300);
        var globalReplenishmentSeconds = GetPositiveIntOrDefault(
            configuration,
            "RateLimiting:Global:ReplenishmentPeriodSeconds",
            60);

        var authPermitLimit = GetPositiveIntOrDefault(
            configuration,
            "RateLimiting:Auth:PermitLimit",
            10);
        var authWindowSeconds = GetPositiveIntOrDefault(
            configuration,
            "RateLimiting:Auth:WindowSeconds",
            60);

        var adminUploadsPermitLimit = GetPositiveIntOrDefault(
            configuration,
            "RateLimiting:AdminUploads:PermitLimit",
            100);
        var adminUploadsWindowSeconds = GetPositiveIntOrDefault(
            configuration,
            "RateLimiting:AdminUploads:WindowSeconds",
            60);
        var webhooksPermitLimit = GetPositiveIntOrDefault(
            configuration,
            "RateLimiting:Webhooks:PermitLimit",
            60);
        var webhooksWindowSeconds = GetPositiveIntOrDefault(
            configuration,
            "RateLimiting:Webhooks:WindowSeconds",
            60);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var ip = GetClientIp(httpContext);

                return RateLimitPartition.GetTokenBucketLimiter(ip, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = globalTokenLimit,
                    TokensPerPeriod = globalTokensPerPeriod,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(globalReplenishmentSeconds),
                    AutoReplenishment = true,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            });

            options.AddPolicy(AuthRateLimitPolicy, httpContext =>
            {
                var ip = GetClientIp(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = authPermitLimit,
                    Window = TimeSpan.FromSeconds(authWindowSeconds),
                    AutoReplenishment = true,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            });

            options.AddPolicy(AdminUploadsRateLimitPolicy, httpContext =>
            {
                var userId = httpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                var key = !string.IsNullOrWhiteSpace(userId)
                    ? $"user:{userId}"
                    : $"ip:{GetClientIp(httpContext)}";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"admin-upload:{key}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = adminUploadsPermitLimit,
                        Window = TimeSpan.FromSeconds(adminUploadsWindowSeconds),
                        AutoReplenishment = true,
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            options.AddPolicy(WebhooksRateLimitPolicy, httpContext =>
            {
                var ip = GetClientIp(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"webhook:{ip}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = webhooksPermitLimit,
                        Window = TimeSpan.FromSeconds(webhooksWindowSeconds),
                        AutoReplenishment = true,
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });
        });

        return services;
    }

    private static int GetPositiveIntOrDefault(
        IConfiguration configuration,
        string key,
        int defaultValue)
    {
        var configuredValue = configuration.GetValue<int?>(key);
        return configuredValue is > 0 ? configuredValue.Value : defaultValue;
    }

    private static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(FrontendCorsPolicy, policy =>
            {
                var allowedOrigins = configuration
                    .GetSection("Cors:AllowedOrigins")
                    .Get<string[]>() ?? [];

                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                }
            });
        });

        return services;
    }

    private static IServiceCollection AddForwardedHeadersSupport(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            var forwardLimit = configuration.GetValue<int?>("ForwardedHeaders:ForwardLimit");
            if (forwardLimit is > 0)
            {
                options.ForwardLimit = forwardLimit.Value;
            }

            var knownProxies = configuration
                .GetSection("ForwardedHeaders:KnownProxies")
                .Get<string[]>() ?? [];

            foreach (var proxy in knownProxies)
            {
                if (IPAddress.TryParse(proxy, out var parsed))
                {
                    options.KnownProxies.Add(parsed);
                }
            }

            var knownNetworks = configuration
                .GetSection("ForwardedHeaders:KnownNetworks")
                .Get<string[]>() ?? [];

            foreach (var network in knownNetworks)
            {
                if (TryParseIpNetwork(network, out var parsed))
                {
                    options.KnownNetworks.Add(parsed);
                }
            }
        });

        return services;
    }

    private static bool TryParseIpNetwork(string? value, out ForwardedIpNetwork network)
    {
        network = default!;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        if (!IPAddress.TryParse(parts[0], out var address))
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var prefixLength))
        {
            return false;
        }

        var maxPrefixLength = address.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
        if (prefixLength < 0 || prefixLength > maxPrefixLength)
        {
            return false;
        }

        network = new ForwardedIpNetwork(address, prefixLength);
        return true;
    }

    private static string GetClientIp(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Hotel Booking API",
                Version = "v1",
                Description = "Hotel Booking Platform — RESTful API"
            });

            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "Enter your JWT token"
            });

            c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            if (File.Exists(xmlPath))
                c.IncludeXmlComments(xmlPath);
        });

        return services;
    }

    private static WebApplication UseDiagnosticsAndErrorHandling(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();

        app.UseSerilogRequestLogging();
        app.UseExceptionHandler();

        return app;
    }

    private static WebApplication UseSwaggerAndHsts(this WebApplication app)
    {
        var swaggerEnabled = app.Configuration.GetValue<bool>("Swagger:Enabled");

        if (app.Environment.IsDevelopment() || swaggerEnabled)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        return app;
    }

    private static WebApplication UseHttpSecurityPipeline(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseHttpsRedirection();
        app.UseCors(FrontendCorsPolicy);
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();

        return app;
    }

    private static IServiceCollection AddExpirePendingPaymentsJobSettings(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<ExpirePendingPaymentsJobSettings>()
            .Bind(configuration.GetSection(ExpirePendingPaymentsJobSettings.SectionName))
            .Validate(s => s.IntervalSeconds > 0, "IntervalSeconds must be > 0.")
            .Validate(s => s.BatchSize > 0, "BatchSize must be > 0.")
            .ValidateOnStart();

        services.AddHostedService<ExpirePendingPaymentsBackgroundService>();

        return services;
    }

    private static IServiceCollection AddCookieSettings(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<CookieSettings>()
            .Bind(configuration.GetSection("CookieSettings"))
            .Validate(s => !string.IsNullOrWhiteSpace(s.RefreshTokenCookieName),
                "CookieSettings:RefreshTokenCookieName is required.")
            .Validate(s => s.RefreshTokenExpiryDays is > 0 and <= 90,
                "CookieSettings:RefreshTokenExpiryDays must be between 1 and 90.")
            .Validate(s => !string.IsNullOrWhiteSpace(s.SameSite),
                "CookieSettings:SameSite is required.")
            .Validate(s => !string.IsNullOrWhiteSpace(s.Path),
                "CookieSettings:Path is required.")
            .ValidateOnStart();

        return services;
    }

    private static IServiceCollection AddHotelImageUploadServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<HotelImageUploadOptions>()
            .Bind(configuration.GetSection(HotelImageUploadOptions.SectionName))
            .Validate(o => o.MaxFileBytes is > 0 and <= 10 * 1024 * 1024, "MaxFileBytes must be between 1 byte and 10 MB.")
            .Validate(o => o.MaxWidth is > 0 and <= 8192, "MaxWidth must be between 1 and 8192.")
            .Validate(o => o.MaxHeight is > 0 and <= 8192, "MaxHeight must be between 1 and 8192.")
            .Validate(o => o.MaxPixels is > 0 and <= 40_000_000, "MaxPixels must be reasonable.")
            .Validate(o => o.JpegQuality is >= 60 and <= 95, "JpegQuality must be between 60 and 95.")
            .Validate(o => o.MaxImagesPerHotel is > 0 and <= 500, "MaxImagesPerHotel must be between 1 and 500.")
            .ValidateOnStart();

        services.AddScoped<IHotelImageUploadProcessor, HotelImageUploadProcessor>();

        return services;
    }
}
