using HotelBooking.Domain.Common.Constants;
using HotelBooking.Infrastructure.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotelBooking.Infrastructure.Identity;

internal sealed class AdminBootstrapService(
    IServiceProvider serviceProvider,
    IOptions<AdminBootstrapSettings> bootstrapOptions,
    ILogger<AdminBootstrapService> logger)
    : IHostedService
{
    private readonly AdminBootstrapSettings _settings = bootstrapOptions.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_settings.Enabled)
            return;

        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        try
        {
            await EnsureRoleExistsAsync(roleManager, cancellationToken);
            await EnsureFirstAdminAsync(userManager);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Admin bootstrap failed.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureRoleExistsAsync(
        RoleManager<IdentityRole<Guid>> roleManager,
        CancellationToken ct)
    {
        if (await roleManager.RoleExistsAsync(HotelBookingConstants.Roles.Admin))
            return;

        var createResult = await roleManager.CreateAsync(new IdentityRole<Guid>
        {
            Id = Guid.CreateVersion7(),
            Name = HotelBookingConstants.Roles.Admin,
            NormalizedName = HotelBookingConstants.Roles.Admin.ToUpperInvariant()
        });

        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create Admin role during bootstrap: {errors}");
        }
    }

    private async Task EnsureFirstAdminAsync(
        UserManager<ApplicationUser> userManager)
    {
        var admins = await userManager.GetUsersInRoleAsync(HotelBookingConstants.Roles.Admin);
        if (admins.Count > 0)
        {
            logger.LogInformation("Admin bootstrap skipped because an Admin account already exists.");
            return;
        }

        var normalizedEmail = _settings.Email.Trim();
        var user = await userManager.FindByEmailAsync(normalizedEmail);

        var isNewUser = false;
        if (user is null)
        {
            isNewUser = true;
            user = new ApplicationUser
            {
                Id = Guid.CreateVersion7(),
                UserName = normalizedEmail,
                Email = normalizedEmail,
                FirstName = _settings.FirstName.Trim(),
                LastName = _settings.LastName.Trim(),
                EmailConfirmed = _settings.EmailConfirmed,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            var createResult = await userManager.CreateAsync(user, _settings.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create bootstrap admin user: {errors}");
            }
        }
        else if (_settings.EmailConfirmed && !user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            var confirmResult = await userManager.UpdateAsync(user);
            if (!confirmResult.Succeeded)
            {
                var errors = string.Join(", ", confirmResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to confirm bootstrap admin email: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, HotelBookingConstants.Roles.Admin))
        {
            var roleAssignResult = await userManager.AddToRoleAsync(user, HotelBookingConstants.Roles.Admin);
            if (!roleAssignResult.Succeeded)
            {
                var errors = string.Join(", ", roleAssignResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to assign Admin role during bootstrap: {errors}");
            }
        }

        logger.LogInformation(
            "Admin bootstrap completed. Admin account {Email} {Action}.",
            normalizedEmail,
            isNewUser ? "created" : "promoted");
    }
}
