using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;
using HotelBooking.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await context.Cities.AnyAsync()) return; // Already seeded

        // ═══ Services (Amenities) ═══
        var services = CreateServices();
        await context.Services.AddRangeAsync(services);

        // ═══ Cities ═══
        var cities = CreateCities();
        await context.Cities.AddRangeAsync(cities);
        await context.SaveChangesAsync();

        // ═══ Room Types (Lookup) ═══
        var roomTypes = CreateRoomTypes();
        await context.RoomTypes.AddRangeAsync(roomTypes);
        await context.SaveChangesAsync();

        // ═══ Hotels + HotelRoomTypes + Rooms + HotelServices ═══
        var (hotels, hotelRoomTypes, rooms, hotelServices, hotelRoomTypeServices) =
            CreateHotelsWithDependencies(cities, roomTypes, services);

        await context.Hotels.AddRangeAsync(hotels);
        await context.SaveChangesAsync();

        await context.HotelRoomTypes.AddRangeAsync(hotelRoomTypes);
        await context.HotelServices.AddRangeAsync(hotelServices);
        await context.SaveChangesAsync();

        await context.Rooms.AddRangeAsync(rooms);
        await context.HotelRoomTypeServices.AddRangeAsync(hotelRoomTypeServices);
        await context.SaveChangesAsync();

        // ═══ Featured Deals ═══
        var deals = CreateFeaturedDeals(hotels, hotelRoomTypes);
        await context.FeaturedDeals.AddRangeAsync(deals);
        await context.SaveChangesAsync();
    }

    private static List<Service> CreateServices()
    {
        var names = new[]
        {
            "Free WiFi", "Swimming Pool", "Spa", "Gym", "Restaurant",
            "Room Service", "Parking", "Airport Shuttle", "Business Center",
            "Laundry", "Bar", "Pet Friendly", "Beach Access", "Kids Club"
        };

        return names.Select(n => new Service(Guid.NewGuid(), n)).ToList();
    }

    private static List<City> CreateCities()
    {
        return
        [
            new(Guid.NewGuid(), "New York",    "USA",     "10001"),
            new(Guid.NewGuid(), "London",      "UK",      "SW1A 1AA"),
            new(Guid.NewGuid(), "Paris",       "France",  "75001"),
            new(Guid.NewGuid(), "Tokyo",       "Japan",   "100-0001"),
            new(Guid.NewGuid(), "Dubai",       "UAE",     "00000"),
            new(Guid.NewGuid(), "Sydney",      "Australia","2000"),
            new(Guid.NewGuid(), "Rome",        "Italy",   "00100"),
            new(Guid.NewGuid(), "Istanbul",    "Turkey",  "34000"),
            new(Guid.NewGuid(), "Barcelona",   "Spain",   "08001"),
            new(Guid.NewGuid(), "Bangkok",     "Thailand","10100"),
        ];
    }

    private static List<RoomType> CreateRoomTypes()
    {
        return
        [
            new(Guid.NewGuid(), "Standard",    "Basic room with essential amenities"),
            new(Guid.NewGuid(), "Superior",    "Enhanced room with better view"),
            new(Guid.NewGuid(), "Deluxe",      "Spacious room with premium amenities"),
            new(Guid.NewGuid(), "Suite",        "Separate living area and bedroom"),
            new(Guid.NewGuid(), "Junior Suite", "Compact suite with sitting area"),
            new(Guid.NewGuid(), "Family Room", "Large room for families"),
            new(Guid.NewGuid(), "Executive",   "Business-class room with desk"),
            new(Guid.NewGuid(), "Penthouse",   "Top floor luxury suite"),
            new(Guid.NewGuid(), "Single",      "Compact room for solo travelers"),
            new(Guid.NewGuid(), "Twin",        "Room with two separate beds"),
        ];
    }

    private static (
        List<Hotel> hotels,
        List<HotelRoomType> hotelRoomTypes,
        List<Room> rooms,
        List<HotelService> hotelServices,
        List<HotelRoomTypeService> hrtServices
    ) CreateHotelsWithDependencies(
        List<City> cities, List<RoomType> roomTypes, List<Service> services)
    {
        var hotels = new List<Hotel>();
        var allHrt = new List<HotelRoomType>();
        var allRooms = new List<Room>();
        var allHotelServices = new List<HotelService>();
        var allHrtServices = new List<HotelRoomTypeService>();

        var hotelNames = new[]
        {
            "Grand Hotel", "Royal Palace", "The Ritz", "Harbor View", "Sunset Inn",
            "Crown Plaza", "Ocean Breeze", "Mountain Lodge", "City Center Hotel", "The Grand"
        };

        var random = new Random(42); // Deterministic seed

        foreach (var city in cities)
        {
            // 5 hotels per city = 50 hotels
            for (int h = 0; h < 5; h++)
            {
                var hotelId = Guid.NewGuid();
                var hotel = new Hotel(
                    hotelId, city.Id,
                    $"{hotelNames[(h + cities.IndexOf(city)) % hotelNames.Length]} {city.Name}",
                    owner: "Hotel Group Inc.",
                    address: $"{random.Next(1, 999)} Main Street, {city.Name}",
                    starRating: (short)random.Next(3, 6),
                    description: $"A beautiful hotel in the heart of {city.Name}",
                    latitude: (decimal)(random.NextDouble() * 180 - 90),
                    longitude: (decimal)(random.NextDouble() * 360 - 180));

                hotels.Add(hotel);

                // 2-4 HotelRoomTypes per hotel
                var numRoomTypes = random.Next(2, 5);
                var selectedRoomTypes = roomTypes
                    .OrderBy(_ => random.Next())
                    .Take(numRoomTypes)
                    .ToList();

                decimal minPrice = decimal.MaxValue;

                foreach (var rt in selectedRoomTypes)
                {
                    var hrtId = Guid.NewGuid();
                    var price = (decimal)(random.Next(80, 500));
                    if (price < minPrice) minPrice = price;

                    var hrt = new HotelRoomType(
                        hrtId, hotelId, rt.Id,
                        pricePerNight: price,
                        adultCapacity: (short)random.Next(1, 4),
                        childCapacity: (short)random.Next(0, 3),
                        description: $"{rt.Name} room at {hotel.Name}");

                    allHrt.Add(hrt);

                    // 3-5 rooms per HotelRoomType
                    var numRooms = random.Next(3, 6);
                    for (int r = 0; r < numRooms; r++)
                    {
                        var floor = (short)(r / 5 + 1);
                        var room = new Room(
                            Guid.NewGuid(), hrtId, hotelId,
                            roomNumber: $"{floor}{(r + 1):D2}",
                            floor: floor);
                        allRooms.Add(room);
                    }
                }

                hotel.UpdatePriceSummary(minPrice);

                // 3-6 HotelServices per hotel
                var numServices = random.Next(3, 7);
                var selectedServices = services
                    .OrderBy(_ => random.Next())
                    .Take(numServices)
                    .ToList();

                foreach (var svc in selectedServices)
                {
                    var hsId = Guid.NewGuid();
                    var hs = new HotelService(hsId, hotelId, svc.Id);
                    allHotelServices.Add(hs);
                }
            }
        }

        return (hotels, allHrt, allRooms, allHotelServices, allHrtServices);
    }

    private static List<FeaturedDeal> CreateFeaturedDeals(
        List<Hotel> hotels, List<HotelRoomType> hotelRoomTypes)
    {
        var deals = new List<FeaturedDeal>();
        var random = new Random(42);

        // Pick 8 random hotels for deals
        var dealHotels = hotels.OrderBy(_ => random.Next()).Take(8).ToList();

        for (int i = 0; i < dealHotels.Count; i++)
        {
            var hotel = dealHotels[i];
            var hrt = hotelRoomTypes.First(x => x.HotelId == hotel.Id);

            var originalPrice = hrt.PricePerNight;
            var discountedPrice = Math.Round(originalPrice * 0.7m, 2); // 30% off

            deals.Add(new FeaturedDeal(
                Guid.NewGuid(), hotel.Id, hrt.Id,
                originalPrice, discountedPrice,
                displayOrder: i + 1));
        }

        return deals;
    }
}