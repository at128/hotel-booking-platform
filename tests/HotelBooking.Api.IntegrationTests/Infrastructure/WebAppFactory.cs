using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Api.IntegrationTests.Fakes;
using HotelBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;
using Testcontainers.MsSql;

using Xunit;
namespace HotelBooking.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Shared WebApplicationFactory for all integration tests.
/// Uses a SINGLE SQL Server container shared across ALL test classes via xUnit Collection Fixture.
/// </summary>
public class WebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly IReadOnlyDictionary<string, string> RequiredTestEnvVars =
        new Dictionary<string, string>
        {
            ["JWT__Secret"] = "integration-tests-jwt-secret-key-1234567890",
            ["JWT__Issuer"] = "HotelBookingApi",
            ["JWT__Audience"] = "HotelBookingClient",
            ["Stripe__SecretKey"] = "sk_test_integration_dummy",
            ["Stripe__WebhookSecret"] = "whsec_integration_dummy",
            ["PaymentUrls__SuccessUrlTemplate"] = "https://test.local/booking/{0}/success",
            ["PaymentUrls__CancelUrlTemplate"] = "https://test.local/booking/{0}/cancel",
            ["Email__SmtpHost"] = "localhost",
            ["Email__SmtpUser"] = "integration-tests",
            ["Email__SmtpPassword"] = "integration-tests",
            ["Email__FromAddress"] = "noreply@test.local",
        };

    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public FakePaymentGateway FakePaymentGateway { get; } = new();
    public FakeEmailService FakeEmailService { get; } = new();

    public WebAppFactory()
    {
        foreach (var item in RequiredTestEnvVars)
        {
            Environment.SetEnvironmentVariable(item.Key, item.Value);
        }
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        foreach (var item in RequiredTestEnvVars)
        {
            Environment.SetEnvironmentVariable(item.Key, null);
        }

        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Keep rate limiting middleware active in tests, but with non-blocking limits.
                ["RateLimiting:Global:TokenLimit"] = "100000",
                ["RateLimiting:Global:TokensPerPeriod"] = "100000",
                ["RateLimiting:Global:ReplenishmentPeriodSeconds"] = "60",
                ["RateLimiting:Auth:PermitLimit"] = "100000",
                ["RateLimiting:Auth:WindowSeconds"] = "60",
                ["RateLimiting:AdminUploads:PermitLimit"] = "100000",
                ["RateLimiting:AdminUploads:WindowSeconds"] = "60",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // 1. Replace DB with Testcontainers SQL Server
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseSqlServer(_dbContainer.GetConnectionString());
            });

            // 2. Mock external services
            services.RemoveAll<IPaymentGateway>();
            services.AddSingleton<IPaymentGateway>(FakePaymentGateway);

            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(FakeEmailService);

            // 3. Stop Background Jobs
            services.RemoveAll<IHostedService>();

            // 4. Keep rate-limiter middleware active, but make limits non-blocking in tests.
            services.RemoveAll<IConfigureOptions<RateLimiterOptions>>();
            services.RemoveAll<IPostConfigureOptions<RateLimiterOptions>>();
            services.Configure<RateLimiterOptions>(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: "tests-global",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 1_000_000,
                            Window = TimeSpan.FromMinutes(1),
                            AutoReplenishment = true,
                            QueueLimit = 0
                        }));

                options.AddPolicy("auth", _ =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: "tests-auth",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 1_000_000,
                            Window = TimeSpan.FromMinutes(1),
                            AutoReplenishment = true,
                            QueueLimit = 0
                        }));

                options.AddPolicy("admin-uploads", _ =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: "tests-admin-uploads",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 1_000_000,
                            Window = TimeSpan.FromMinutes(1),
                            AutoReplenishment = true,
                            QueueLimit = 0
                        }));
            });

            // 5. Apply Migrations
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        });
    }

    /// <summary>
    /// Creates a scoped AppDbContext for seeding/asserting in tests
    /// </summary>
    public AppDbContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }
}
