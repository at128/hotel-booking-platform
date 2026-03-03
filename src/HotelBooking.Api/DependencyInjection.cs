using HotelBooking.Api.BackgroundJobs;
using HotelBooking.Api.Infrastructure;
using HotelBooking.Api.Services;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Infrastructure.Settings;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Threading.RateLimiting;

namespace HotelBooking.Api;

public static class DependencyInjection
{
    private const string FrontendCorsPolicy = "Frontend";
    private const string AuthRateLimitPolicy = "auth";

    public static IServiceCollection AddPresentation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPresentationCore();
        services.AddApiVersioningServices();
        services.AddSwaggerDocumentation();
        services.AddRateLimitingPolicies();
        services.AddCorsPolicy(configuration);
        services.AddControllers();
        services.AddEndpointsApiExplorer();

        services.AddHttpContextAccessor();
        services.AddScoped<IUser, CurrentUser>();
        services.AddScoped<ICookieService, CookieService>(); // ← أضف هذا

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddMemoryCache();
        services.Configure<CookieSettings>(
        configuration.GetSection("CookieSettings"));

        services.AddExpirePendingPaymentsJobSettings(configuration);

        
        return services;
    }

    public static WebApplication UseCoreMiddlewares(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseDiagnosticsAndErrorHandling();
        app.UseSwaggerAndHsts();
        app.UseHttpSecurityPipeline();

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

    private static IServiceCollection AddRateLimitingPolicies(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var ip = GetClientIp(httpContext);

                return RateLimitPartition.GetTokenBucketLimiter(ip, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 300,
                    TokensPerPeriod = 300,
                    ReplenishmentPeriod = TimeSpan.FromMinutes(1),
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
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    AutoReplenishment = true,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            });
        });

        return services;
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
                          .AllowAnyMethod();
                }
            });
        });

        return services;
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
        app.UseHttpsRedirection();
        app.UseCors(FrontendCorsPolicy);
        app.UseRateLimiter();

        app.UseAuthentication();
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
}