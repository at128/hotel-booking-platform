using System.Text;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Settings;
using HotelBooking.Infrastructure.Data;
using HotelBooking.Infrastructure.Data.Interceptors;
using HotelBooking.Infrastructure.Identity;
using HotelBooking.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace HotelBooking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        var (jwtSection, jwtSecret) = GetAndValidateJwtConfiguration(configuration);

        services.Configure<JwtSettings>(jwtSection);

        services.AddBookingSettingsOptions(configuration);
        services.AddPersistence(configuration);
        services.AddIdentityServices();
        services.AddJwtAuthentication(jwtSection, jwtSecret);

        services.AddAuthorization();

        services.AddScoped<ITokenProvider, TokenProvider>();
        services.AddScoped<IIdentityService, IdentityService>();

        return services;
    }

    private static (IConfigurationSection JwtSection, string Secret) GetAndValidateJwtConfiguration(
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("JWT");
        var secret = jwtSection["Secret"];

        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT:Secret must be configured and at least 32 characters.");
        }

        return (jwtSection, secret);
    }

    private static IServiceCollection AddBookingSettingsOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<BookingSettings>()
            .Bind(configuration.GetSection("BookingSettings"))
            .Validate(s => s.CheckoutHoldMinutes is >= 1 and <= 120,
                "BookingSettings:CheckoutHoldMinutes must be between 1 and 120 minutes.")
            .Validate(s => s.TaxRate is >= 0m and <= 1m,
                "BookingSettings:TaxRate must be between 0 and 1.")
            .Validate(s => s.CancellationFreeHours is >= 0 and <= 720,
                "BookingSettings:CancellationFreeHours must be between 0 and 720 hours.")
            .Validate(s => s.CancellationFeePercent is >= 0m and <= 1m,
                "BookingSettings:CancellationFeePercent must be between 0 and 1.")
            .Validate(s => s.MaxAdvanceBookingDays is >= 1 and <= 3650,
                "BookingSettings:MaxAdvanceBookingDays must be between 1 and 3650 days.")
            .ValidateOnStart();

        return services;
    }

    private static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());

            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql =>
                {
                    sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    sql.EnableRetryOnFailure(3);
                });
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        return services;
    }

    private static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;

            options.User.RequireUniqueEmail = true;

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }

    private static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfigurationSection jwtSection,
        string jwtSecret)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtSection["Issuer"],
                    ValidAudience = jwtSection["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSecret)),

                    ClockSkew = TimeSpan.Zero
                };
            });

        return services;
    }
}