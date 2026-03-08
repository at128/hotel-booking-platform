# Hotel Booking Platform — Unit Test Suite

## Quick Setup

```bash
# From the solution root (where your .sln file is):
cd tests

# Restore and build
dotnet restore
dotnet build

# Run all tests
dotnet test

# Run with coverage report
dotnet test --collect:"XPlat Code Coverage"
```

## Project Structure

```
tests/
├── HotelBooking.Domain.Tests/          (80 tests)
│   ├── Bookings/
│   │   ├── BookingTests.cs              — Booking state machine (17 tests)
│   │   ├── PaymentTests.cs              — Payment state machine (29 tests)
│   │   ├── CheckoutHoldTests.cs         — Hold expiry + release (3 tests)
│   │   └── CancellationTests.cs         — Refund status transitions (4 tests)
│   ├── Hotels/
│   │   └── HotelEntityTests.cs          — City, Hotel, FeaturedDeal (15 tests)
│   ├── Cart/
│   │   └── CartItemAndHotelVisitTests.cs — CartItem, HotelVisit (4 tests)
│   └── Common/
│       └── ResultTests.cs               — Result<T> + Error (8 tests)
│
└── HotelBooking.Application.Tests/      (210 tests)
    ├── _Shared/
    │   └── TestHelpers.cs               — Entity factories + mock helpers
    ├── Admin/
    │   ├── AdminCityHandlerTests.cs     — City CRUD handlers (10 tests)
    │   ├── AdminHotelRoomTypeServiceHandlerTests.cs — Hotel/RoomType/Service (19 tests)
    │   └── AdminValidatorTests.cs       — All admin validators (28 tests)
    ├── Auth/
    │   ├── AuthHandlerTests.cs          — Register/Login/Refresh/Logout (25 tests)
    │   └── AuthValidatorTests.cs        — Register/Login validators (25 tests)
    ├── Cart/
    │   └── CartAndBehaviorTests.cs      — AddToCart + MediatR behaviors (12 tests)
    ├── Checkout/
    │   └── CheckoutTests.cs             — CreateHold/Booking/Webhook/Cancel/Expire (45 tests)
    └── MissingHandlers/
        └── MissingHandlerTests.cs       — Home/Hotels/Events/Reviews/Search (46 tests)
```

## Total: 290 tests

## Build Error Fixes Applied

1. **`Room` constructor** — Removed `status: RoomStatus.Available` parameter from 
   `TestHelpers.CreateRoom()`. The actual `Room` constructor has only 
   `(id, hotelRoomTypeId, hotelId, roomNumber, floor?)`.

2. **Unified `TestHelpers`** — Merged two versions (Domain steps + Checkout steps) into 
   one file with consistent factory methods, navigation property helpers, and mock utilities.

## Notes

- Add both test projects to your `.sln` file
- The csproj files reference `..\..\src\...` — adjust paths if your folder structure differs
- Tests use Moq + MockQueryable.Moq for DbSet mocking
- All assertions use FluentAssertions
