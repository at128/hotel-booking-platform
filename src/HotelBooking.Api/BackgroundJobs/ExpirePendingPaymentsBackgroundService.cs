using HotelBooking.Application.Features.Checkout.Commands.ExpirePendingPayments;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotelBooking.Api.BackgroundJobs;

public sealed class ExpirePendingPaymentsBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ExpirePendingPaymentsJobSettings> options,
    ILogger<ExpirePendingPaymentsBackgroundService> logger)
    : BackgroundService
{
    private readonly ExpirePendingPaymentsJobSettings _settings = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            logger.LogInformation("ExpirePendingPaymentsBackgroundService is disabled.");
            return;
        }

        logger.LogInformation(
            "ExpirePendingPaymentsBackgroundService started. IntervalSeconds={IntervalSeconds}, BatchSize={BatchSize}",
            _settings.IntervalSeconds,
            _settings.BatchSize);

        // Run once immediately on startup (optional but useful)
        await RunOnceSafeAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.IntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var hasTick = await timer.WaitForNextTickAsync(stoppingToken);
                if (!hasTick)
                    break;

                await RunOnceSafeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("ExpirePendingPaymentsBackgroundService stopped.");
    }

    private async Task RunOnceSafeAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            var result = await sender.Send(
                new ExpirePendingPaymentsCommand(_settings.BatchSize),
                ct);

            if (result.IsError)
            {
                // Avoid assuming FirstError/TopError naming here for compatibility
                logger.LogWarning(
                    "ExpirePendingPayments job returned a business error. The run was acknowledged and will retry on next interval.");
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Unhandled exception in ExpirePendingPaymentsBackgroundService loop");
        }
    }
}