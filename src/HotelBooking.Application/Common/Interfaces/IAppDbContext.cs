using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Cart;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Reviews;
using HotelBooking.Domain.Rooms;
using HotelBooking.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
namespace HotelBooking.Application.Common.Interfaces;
public interface IAppDbContext
{
    DbSet<City> Cities { get; }
    DbSet<Hotel> Hotels { get; }
    DbSet<HotelRoomType> HotelRoomTypes { get; }
    DbSet<HotelService> HotelServices { get; }
    DbSet<HotelRoomTypeService> HotelRoomTypeServices { get; }
    DbSet<FeaturedDeal> FeaturedDeals { get; }
    DbSet<HotelVisit> HotelVisits { get; }
    DbSet<RoomType> RoomTypes { get; }
    DbSet<Room> Rooms { get; }
    DbSet<Service> Services { get; }
    DbSet<Image> Images { get; }
    public DbSet<Booking> Bookings { get; }
    public DbSet<CheckoutHold> CheckoutHolds { get; }
    public DbSet<BookingRoom> BookingRooms { get; }
    DbSet<BookingService> BookingServices { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Cancellation> Cancellations { get; }

    DbSet<CartItem> CartItems { get; }

    DbSet<Review> Reviews { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct);
    Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken ct);

    Task ReloadEntityAsync<TEntity>(TEntity entity, CancellationToken ct) where TEntity : class;
    void ClearChangeTracker();

}