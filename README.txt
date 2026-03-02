# 🏨 Hotel Booking Platform API

A backend API for hotel booking built with **ASP.NET Core 8**, following **Clean Architecture** and **CQRS** patterns. The system handles hotel search, room availability, cart management, and checkout with atomic inventory holds.

> ⚠️ **Status: In Progress** — Core features (auth, search, cart, checkout holds) are implemented. Booking confirmation, payment processing, and admin panel are under active development.

---

## Tech Stack

| Layer | Technologies |
|---|---|
| **Framework** | ASP.NET Core 8, .NET 9 |
| **Architecture** | Clean Architecture, CQRS (MediatR), Result Pattern |
| **Database** | SQL Server, Entity Framework Core (Code-First) |
| **Auth** | JWT + Refresh Token Rotation with Reuse Detection |
| **Caching** | HybridCache with tag-based invalidation |
| **Logging** | Serilog + Seq |
| **Testing** | xUnit, FluentAssertions, NSubstitute |
| **DevOps** | Docker Compose, GitHub Actions CI/CD |

---

## Architecture

```
src/
├── HotelBooking.Domain           # Entities, value objects, domain errors
├── HotelBooking.Application      # CQRS commands/queries, validators, pipeline behaviors
├── HotelBooking.Contracts         # Request/response DTOs (shared with clients)
├── HotelBooking.Infrastructure   # EF Core, Identity, JWT, repositories
└── HotelBooking.Api              # Controllers, middleware, DI configuration

tests/
├── HotelBooking.Domain.UnitTests
├── HotelBooking.Application.UnitTests
├── HotelBooking.Application.SubcutaneousTests
├── HotelBooking.Api.IntegrationTests
└── HotelBooking.Tests.Common
```

The project enforces strict dependency rules — inner layers have zero knowledge of outer layers. The Domain layer has no external dependencies.

---

## API Endpoints

### Auth (`/api/v1/auth`)
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/register` | — | Create a new account |
| POST | `/login` | — | Login and receive JWT + refresh token |
| POST | `/refresh` | — | Rotate refresh token and get new JWT |
| GET | `/profile` | 🔒 | Get current user profile |
| PUT | `/profile` | 🔒 | Update profile info |
| POST | `/logout` | 🔒 | Logout current session |
| POST | `/logout-all` | 🔒 | Revoke all sessions |

### Home (`/api/v1/home`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/featured-deals` | Active promotional deals (cached 5 min) |
| GET | `/trending-cities` | Top 5 cities by visit count (cached 10 min) |
| GET | `/config` | Search defaults and amenities list |

### Search (`/api/v1/search`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/hotels` | Search with filters: city, dates, guests, price range, star rating, amenities. Cursor-based pagination. |

### Cart (`/api/v1/cart`) 🔒
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | Get current cart |
| POST | `/items` | Add room type to cart |
| PUT | `/items/{id}` | Update cart item |
| DELETE | `/items/{id}` | Remove cart item |
| DELETE | `/` | Clear entire cart |

### Events (`/api/v1/events`) 🔒
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/hotel-viewed` | Track hotel page view |
| GET | `/recently-visited` | Get recently visited hotels |

### Health
| Endpoint | Description |
|----------|-------------|
| `/api/v1/health/live` | Liveness probe (no deps check) |
| `/api/v1/health/ready` | Readiness probe (checks DB) 🔒 |

---

## Key Implementation Details

### Refresh Token Security
The auth system implements **refresh token rotation with reuse detection**. Each refresh token belongs to a "family" — if a previously-used token is presented (indicating potential theft), the entire family is revoked, forcing re-login on all sessions. Tokens are stored as SHA-256 hashes and delivered via HTTP-only cookies. A background service cleans up expired tokens automatically.

### Atomic Checkout Holds
Room availability during checkout uses **SERIALIZABLE isolation level** transactions. When a user proceeds to checkout, the system acquires holds on the requested rooms atomically — if any room type is unavailable, the entire operation rolls back. This prevents two users from booking the last available room simultaneously.

### MediatR Pipeline
All requests pass through a pipeline of cross-cutting behaviors:
- **ValidationBehavior** — Runs FluentValidation rules before the handler executes
- **CachingBehavior** — HybridCache with tag-based invalidation for query results
- **PerformanceBehavior** — Logs slow-running requests for monitoring
- **UnhandledExceptionBehavior** — Catches and logs unexpected failures

### Error Handling
The API uses a **Result pattern** instead of throwing exceptions for expected errors. Controllers map error types to appropriate HTTP status codes via a centralized `Problem()` method. A `GlobalExceptionHandler` catches unexpected exceptions and returns standardized ProblemDetails responses.

---

## Getting Started

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for containerized setup)
- SQL Server (for local setup without Docker)

### Option 1: Docker Compose (Recommended)

```bash
# Clone the repository
git clone https://github.com/at128/hotel-booking-platform.git
cd hotel-booking-platform

# Create .env file
cat > .env << EOF
SA_PASSWORD=YourStrong!Password123
JWT_SECRET=your-secret-key-at-least-32-characters-long-here
SEQ_ADMIN_PASSWORD=seqadminpassword
STRIPE_SECRET_KEY=sk_test_placeholder
STRIPE_WEBHOOK_SECRET=whsec_placeholder
EOF

# Start all services
docker compose up -d

# API will be available at http://localhost:5000
# Swagger UI at http://localhost:5000/swagger
# Seq dashboard at http://localhost:5341
```

### Option 2: Local Development

```bash
# Clone and navigate
git clone https://github.com/at128/hotel-booking-platform.git
cd hotel-booking-platform

# Update connection string in appsettings.json to point to your SQL Server

# Set JWT secret (required - minimum 32 characters)
dotnet user-secrets set "JWT:Secret" "your-secret-key-at-least-32-characters-long-here" --project src/HotelBooking.Api

# Run the API
dotnet run --project src/HotelBooking.Api

# The API will apply migrations and seed data automatically in Development mode
```

### Running Tests

```bash
dotnet test --filter "Category!=Integration&Category!=E2E"
```

---

## Roadmap

- [x] Authentication (JWT + Refresh Token Rotation)
- [x] Hotel Search with filters and pagination
- [x] Home page (featured deals, trending cities)
- [x] Cart Management (CRUD)
- [x] Checkout Holds (atomic room reservation)
- [x] Recently Visited Hotels tracking
- [x] Docker Compose setup
- [x] CI/CD Pipeline (GitHub Actions)
- [x] Unit Tests (auth flows, validators)
- [ ] Booking confirmation and payment processing
- [ ] Admin management panel
- [ ] Email notifications
- [ ] Integration tests

---

## License

This project is for educational and portfolio purposes.
