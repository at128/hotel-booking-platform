using HotelBooking.Api;
using HotelBooking.Application;
using HotelBooking.Domain.Common.Constants;
using HotelBooking.Infrastructure;
using HotelBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

ConfigureHostLogging(builder);
ValidateProductionSecrets(builder);
AddApplicationServices(builder);
AddHealthChecks(builder);



var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    await ApplyMigrationsAndSeedAsync(app);
}


app.UseCoreMiddlewares();
app.MapControllers();
MapHealthEndpoints(app);

Log.Information("Starting Hotel Booking API...");
app.Run();

static void ConfigureHostLogging(WebApplicationBuilder builder)
{
    builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));
}

static void AddApplicationServices(WebApplicationBuilder builder)
{
    builder.Services
        .AddPresentation(builder.Configuration)
        .AddApplication()
        .AddInfrastructure(builder.Configuration);
}

static void AddHealthChecks(WebApplicationBuilder builder)
{
    builder.Services.AddHealthChecks()
        .AddSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection")!,
            name: "database",
            timeout: TimeSpan.FromSeconds(3));
}

static async Task ApplyMigrationsAndSeedAsync(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if ((await context.Database.GetPendingMigrationsAsync()).Any())
        {
            app.Logger.LogWarning(
                "Applying {Count} pending migrations...",
                (await context.Database.GetPendingMigrationsAsync()).Count());

            await context.Database.MigrateAsync();
        }

        await DataSeeder.SeedAsync(app.Services);
    }
    else
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            await context.Database.CanConnectAsync();
            app.Logger.LogInformation("Database connection verified.");
        }
        catch (Exception ex)
        {
            app.Logger.LogCritical(ex, "Cannot connect to database on startup.");
            throw;
        }
    }
}
static void MapHealthEndpoints(WebApplication app)
{
    var allowAnonymousHealthEndpoints =
        app.Configuration.GetValue<bool>("Monitoring:AllowAnonymousHealthEndpoints");

    var liveEndpoint = app.MapHealthChecks("/api/v1/health/live", new HealthCheckOptions
    {
        Predicate = _ => false,
        AllowCachingResponses = false
    });

    var readyEndpoint = app.MapHealthChecks("/api/v1/health/ready", new HealthCheckOptions
    {

        Predicate = _ => true,
        AllowCachingResponses = false
    });

    if (allowAnonymousHealthEndpoints)
    {
        liveEndpoint.AllowAnonymous();
        readyEndpoint.AllowAnonymous();
        return;
    }

    var adminOnly = new AuthorizeAttribute
    {
        Roles = HotelBookingConstants.Roles.Admin
    };

    liveEndpoint.RequireAuthorization(adminOnly);
    readyEndpoint.RequireAuthorization(adminOnly);
}

static void ValidateProductionSecrets(WebApplicationBuilder builder)
{
    if (!builder.Environment.IsProduction())
        return;

    var failures = new List<string>();

    ValidateSecret(builder.Configuration, "JWT:Secret", minLength: 32, failures);
    ValidateSecret(builder.Configuration, "Stripe:SecretKey", minLength: 8, failures);
    ValidateSecret(builder.Configuration, "Stripe:WebhookSecret", minLength: 8, failures);
    ValidateSecret(builder.Configuration, "Email:SmtpPassword", minLength: 8, failures);

    var stripeSecret = builder.Configuration["Stripe:SecretKey"];
    if (!string.IsNullOrWhiteSpace(stripeSecret) &&
        stripeSecret.StartsWith("sk_test_", StringComparison.OrdinalIgnoreCase))
    {
        failures.Add("Stripe:SecretKey must be a live key in Production (sk_live_...).");
    }

    if (failures.Count == 0)
        return;

    throw new InvalidOperationException(
        "Production secret validation failed: " + string.Join(" | ", failures));
}

static void ValidateSecret(
    IConfiguration configuration,
    string key,
    int minLength,
    List<string> failures)
{
    var value = configuration[key];

    if (string.IsNullOrWhiteSpace(value) || value.Length < minLength)
    {
        failures.Add($"{key} is missing or too short.");
        return;
    }

    if (LooksLikePlaceholder(value))
    {
        failures.Add($"{key} looks like a placeholder and must be replaced.");
    }
}

static bool LooksLikePlaceholder(string value)
{
    var normalized = value.Trim().ToLowerInvariant();

    return normalized.Contains("changeme", StringComparison.Ordinal)
        || normalized.Contains("replace_me", StringComparison.Ordinal)
        || normalized.Contains("placeholder", StringComparison.Ordinal)
        || normalized.Contains("dummy", StringComparison.Ordinal)
        || normalized.Contains("integration-tests", StringComparison.Ordinal)
        || normalized.Contains("example", StringComparison.Ordinal);
}

public partial class Program;
