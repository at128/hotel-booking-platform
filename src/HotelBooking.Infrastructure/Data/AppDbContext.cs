using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Cart;
using HotelBooking.Domain.Common;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Reviews;
using HotelBooking.Domain.Rooms;
using HotelBooking.Domain.Services;
using HotelBooking.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, IMediator mediator)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IAppDbContext
{

    public DbSet<City> Cities => Set<City>();
    public DbSet<Hotel> Hotels => Set<Hotel>();
    public DbSet<HotelRoomType> HotelRoomTypes => Set<HotelRoomType>();
    public DbSet<HotelService> HotelServices => Set<HotelService>();
    public DbSet<HotelRoomTypeService> HotelRoomTypeServices => Set<HotelRoomTypeService>();
    public DbSet<FeaturedDeal> FeaturedDeals => Set<FeaturedDeal>();
    public DbSet<HotelVisit> HotelVisits => Set<HotelVisit>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Image> Images => Set<Image>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<CheckoutHold> CheckoutHolds => Set<CheckoutHold>();
    public DbSet<BookingRoom> BookingRooms => Set<BookingRoom>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Cancellation> Cancellations => Set<Cancellation>();
    public DbSet<BookingService> BookingServices => Set<BookingService>();
    public DbSet<Review> Reviews => Set<Review>();
    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        await DispatchDomainEventsAsync(ct);
        return await base.SaveChangesAsync(ct);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); 

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        
        var adminRoleId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var userRoleId = Guid.Parse("10000000-0000-0000-0000-000000000002");

        builder.Entity<IdentityRole<Guid>>().HasData(
            new IdentityRole<Guid>
            {
                Id = adminRoleId,
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = adminRoleId.ToString()
            },
            new IdentityRole<Guid>
            {
                Id = userRoleId,
                Name = "User",
                NormalizedName = "USER",
                ConcurrencyStamp = userRoleId.ToString()
            }
        );

        // Soft delete filter (ISoftDeletable)
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(ApplySoftDeleteFilter),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);
                method.Invoke(null, [builder]);
            }
        }
    }

    private static void ApplySoftDeleteFilter<T>(ModelBuilder builder) where T : class, ISoftDeletable
    {
        builder.Entity<T>().HasQueryFilter(e => e.DeletedAtUtc == null);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken ct)
    {
        var entities = ChangeTracker.Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entities.SelectMany(e => e.DomainEvents).ToList();

        foreach (var entity in entities)
            entity.ClearDomainEvents();

        foreach (var domainEvent in domainEvents)
            await mediator.Publish(domainEvent, ct);
    }
}
