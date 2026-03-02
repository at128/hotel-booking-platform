using HotelBooking.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Infrastructure.BackgroundJobs;

public sealed class RefreshTokenCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<RefreshTokenCleanupService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromMinutes(5), ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

                await repo.RemoveExpiredAsync(ct);

                logger.LogInformation("Refresh token cleanup completed successfully.");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Refresh token cleanup failed.");
            }

            await Task.Delay(TimeSpan.FromHours(6), ct);
        }
    }
}