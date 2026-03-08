using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Cart;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Hotels.Enums;
using HotelBooking.Domain.Reviews;
using HotelBooking.Domain.Rooms;
using HotelBooking.Domain.Services;
using HotelBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelBooking.Api.IntegrationTests.Helpers;

public static class SeedHelper
{
    public static async Task<SeedResult> SeedFullHierarchy(AppDbContext db)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var city = new City(
            Guid.NewGuid(),
            $"Amman-{suffix}",
            "Jordan",
            "11937");

        var hotel = new Hotel(
            Guid.NewGuid(),
            city.Id,
            $"Grand Palace Hotel {suffix}",
            "Test Owner",
            "123 Main St",
            5,
            "A luxury hotel",
            31.95m,
            35.93m);

        var roomType = new RoomType(
            Guid.NewGuid(),
            $"Deluxe Suite {suffix}",
            "A deluxe suite");

        var hotelRoomType = new HotelRoomType(
            Guid.NewGuid(),
            hotel.Id,
            roomType.Id,
            pricePerNight: 150.00m,
            adultCapacity: 2,
            childCapacity: 1,
            description: "Deluxe suite with city view");

        var rooms = new List<Room>(capacity: 5);
        for (int i = 1; i <= 5; i++)
        {
            var room = new Room(
                Guid.NewGuid(),
                hotelRoomType.Id,
                hotel.Id,
                $"{suffix[..3]}-{100 + i}",
                (short)i);

            rooms.Add(room);
        }

        db.Cities.Add(city);
        db.Hotels.Add(hotel);
        db.RoomTypes.Add(roomType);
        db.HotelRoomTypes.Add(hotelRoomType);
        db.Rooms.AddRange(rooms);
        await db.SaveChangesAsync();

        return new SeedResult(city, hotel, roomType, hotelRoomType, rooms);
    }

    public static async Task<FeaturedDeal> SeedFeaturedDeal(
        AppDbContext db, Guid hotelId, Guid hotelRoomTypeId)
    {
        var existing = await db.FeaturedDeals
            .FirstOrDefaultAsync(x =>
                x.HotelId == hotelId &&
                x.HotelRoomTypeId == hotelRoomTypeId);

        if (existing is not null)
            return existing;

        var deal = new FeaturedDeal(
            Guid.NewGuid(), hotelId, hotelRoomTypeId,
            originalPrice: 200m, discountedPrice: 150m,
            displayOrder: 1);

        db.FeaturedDeals.Add(deal);
        await db.SaveChangesAsync();
        return deal;
    }

    public static async Task<HotelVisit> SeedHotelVisit(
        AppDbContext db, Guid userId, Guid hotelId)
    {
        var existing = await db.HotelVisits
            .FirstOrDefaultAsync(x => x.UserId == userId && x.HotelId == hotelId);

        if (existing is not null)
            return existing;

        var visit = new HotelVisit(Guid.NewGuid(), userId, hotelId);
        visit.UpdateVisitTime();
        db.HotelVisits.Add(visit);
        await db.SaveChangesAsync();
        return visit;
    }

    public static async Task<Image> SeedHotelImage(
        AppDbContext db, Guid hotelId, string url = "/images/test.jpg")
    {
        var existing = await db.Images
            .FirstOrDefaultAsync(x =>
                x.EntityType == ImageType.Hotel &&
                x.EntityId == hotelId &&
                x.Url == url);

        if (existing is not null)
            return existing;

        var image = new Image(Guid.NewGuid(), ImageType.Hotel, hotelId, url, "Test Image", 0);
        db.Images.Add(image);
        await db.SaveChangesAsync();
        return image;
    }

    public static async Task<Service> SeedService(AppDbContext db, string name = "WiFi")
    {
        var existing = await db.Services
            .FirstOrDefaultAsync(x => x.Name == name);

        if (existing is not null)
            return existing;

        var service = new Service(Guid.NewGuid(), name, "Free WiFi");
        db.Services.Add(service);
        await db.SaveChangesAsync();
        return service;
    }

    /// <summary>
    /// Creates a confirmed booking directly in the DB for testing.
    /// </summary>
    public static async Task<Booking> SeedConfirmedBooking(
        AppDbContext db,
        Guid userId,
        Hotel hotel,
        HotelRoomType hotelRoomType,
        Room room,
        DateOnly checkIn,
        DateOnly checkOut,
        string userEmail = "testuser@test.com")
    {
        var existing = await db.Bookings
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.HotelId == hotel.Id &&
                x.CheckIn == checkIn &&
                x.CheckOut == checkOut &&
                x.Status == BookingStatus.Confirmed);

        if (existing is not null)
            return existing;

        var nights = checkOut.DayNumber - checkIn.DayNumber;
        var total = hotelRoomType.PricePerNight * nights;

        var booking = new Booking(
            Guid.NewGuid(),
            $"BK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20],
            userId,
            hotel.Id,
            hotel.Name,
            hotel.Address,
            userEmail,
            checkIn,
            checkOut,
            total);

        db.Bookings.Add(booking);

        var bookingRoom = new BookingRoom(
            Guid.NewGuid(),
            booking.Id,
            hotel.Id,
            room.Id,
            hotelRoomType.Id,
            hotelRoomType.RoomType?.Name ?? "Deluxe Suite",
            room.RoomNumber,
            hotelRoomType.PricePerNight);

        db.BookingRooms.Add(bookingRoom);

        var transactionRef = $"txn_test_{Guid.NewGuid():N}";
        var providerSession = $"fake_session_{Guid.NewGuid():N}";

        var payment = new Payment(
            Guid.NewGuid(),
            booking.Id,
            total,
            PaymentMethod.Stripe,
            transactionRef);

        payment.SetProviderSession(providerSession);
        payment.MarkAsSucceeded(transactionRef);
        db.Payments.Add(payment);

        await db.SaveChangesAsync();

        booking.Confirm();
        await db.SaveChangesAsync();

        return booking;
    }
}

public record SeedResult(
    City City,
    Hotel Hotel,
    RoomType RoomType,
    HotelRoomType HotelRoomType,
    List<Room> Rooms);
