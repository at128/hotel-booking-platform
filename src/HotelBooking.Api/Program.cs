using HotelBooking.Api;
using HotelBooking.Application;
using HotelBooking.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

builder.Services
    .AddPresentation()
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "database", timeout: TimeSpan.FromSeconds(3));

var app = builder.Build();

app.UseCoreMiddlewares();
app.MapControllers();
app.MapHealthChecks("/api/v1/health");

Log.Information("Starting Hotel Booking API...");
app.Run();

public partial class Program;