using HotelBooking.Api;
using HotelBooking.Application;
using HotelBooking.Infrastructure;
using HotelBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

ConfigureHostLogging(builder);
AddApplicationServices(builder);
AddHealthChecks(builder);

var app = builder.Build();

await ApplyMigrationsAndSeedAsync(app);

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
    app.MapHealthChecks("/api/v1/health/live", new HealthCheckOptions
    {
        Predicate = _ => false,
        AllowCachingResponses = false
    });

    app.MapHealthChecks("/api/v1/health/ready", new HealthCheckOptions
    {
        Predicate = _ => true,
        AllowCachingResponses = false
    })
    .RequireAuthorization();
}

public partial class Program;