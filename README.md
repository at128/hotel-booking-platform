# Hotel Booking Platform API

A production-oriented hotel booking backend built with ASP.NET Core 9 and Clean Architecture.

It covers:
- user authentication with JWT + refresh token rotation
- hotel discovery and search (Elasticsearch + SQL fallback)
- cart, checkout holds, booking creation, Stripe payment session generation
- Stripe webhooks, cancellation/refund workflows
- full admin APIs for hotel inventory/content management
- rate limiting, security headers, logging, health checks, and CI pipeline

## Table Of Contents
- [Project Scope](#project-scope)
- [Architecture](#architecture)
- [Core Flows](#core-flows)
- [API Basics](#api-basics)
- [Endpoint Catalog](#endpoint-catalog)
- [Rate Limiting](#rate-limiting)
- [Configuration](#configuration)
- [Run With Docker](#run-with-docker)
- [Run Locally](#run-locally)
- [Seeding And Admin Bootstrap](#seeding-and-admin-bootstrap)
- [Testing](#testing)
- [CI](#ci)
- [Monitoring And Security](#monitoring-and-security)
- [Useful Docs](#useful-docs)

## Project Scope

### Tech Stack
- .NET 9 / ASP.NET Core 9
- Entity Framework Core + SQL Server
- MediatR (CQRS), FluentValidation
- ASP.NET Core Identity + JWT bearer auth
- Stripe.net (payments/webhooks/refunds)
- HybridCache (with tags)
- Serilog + Seq
- Elasticsearch + optional semantic search via local embedding service (FastAPI + sentence-transformers)
- xUnit + FluentAssertions + Testcontainers

### Main Modules
```
src/
  HotelBooking.Api              Controllers, middleware, rate limiting, DI
  HotelBooking.Application      Commands/queries, validators, pipeline behaviors
  HotelBooking.Domain           Core entities, enums, business rules, result model
  HotelBooking.Infrastructure   EF Core, Identity, Stripe, Email, Elasticsearch, caching
  HotelBooking.Contracts        Request/response DTOs

tests/
  HotelBooking.Domain.Tests
  HotelBooking.Application.Tests
  HotelBooking.Api.IntegrationTests
```

## Architecture

The project follows Clean Architecture:
- `Domain` is independent (no infrastructure dependencies).
- `Application` defines use-cases and interfaces (`IAppDbContext`, `IPaymentGateway`, etc.).
- `Infrastructure` implements persistence, auth, Stripe, email, Elasticsearch, cache invalidation.
- `Api` handles transport concerns (controllers, auth, HTTP policies, middleware, routing, versioning).

Cross-cutting MediatR pipeline behaviors:
- validation behavior
- performance behavior (warns for requests over 500ms)
- unhandled exception behavior
- caching behavior (for cached queries implementing `ICachedQuery`)

## Core Flows

### 1) Authentication And Sessions
- Register/login issues access token (JWT) + refresh token cookie (`HttpOnly`).
- Refresh token is stored hashed (SHA-256), rotated on refresh, and protected with reuse detection.
- If an already-used refresh token is presented, the whole token family is revoked.

### 2) Search And Discovery
- `/api/v1/search` tries Elasticsearch first.
- If Elasticsearch is unavailable or fails, handler falls back to SQL query path.
- Supports cursor pagination and multiple filters.
- Optional semantic/hybrid search when embeddings service is reachable.

### 3) Cart -> Hold -> Booking -> Payment
- Cart contains room selections with dates and guest counts.
- `POST /checkout/hold` creates temporary inventory holds (single hotel constraint).
- `POST /checkout/booking` creates pending booking + payment record, then creates Stripe checkout session.
- Booking creation uses transactional + compensating pattern to avoid partial failures.

### 4) Webhook Confirmation
- `POST /webhooks/stripe` validates Stripe signature and processes event idempotently.
- Handles duplicate/out-of-order webhook events safely.
- On success: payment becomes succeeded, booking confirmed, confirmation email attempted.

### 5) Cancellation And Refund
- Cancellation allowed only for confirmed future bookings.
- Refund amount is based on configured free window and fee percent.
- Free window is calculated from the successful payment timestamp (not booking creation timestamp).
- Refund execution is idempotent and uses stable idempotency key per cancellation.

### 6) Background Jobs
- `ExpirePendingPaymentsBackgroundService` expires old pending/failed-initiation payments.
- `RefreshTokenCleanupService` periodically deletes old expired refresh tokens.
- `ElasticsearchSyncBackgroundService` initializes index and performs full + incremental sync.

## API Basics

- Base path format: `/api/v{version}/...`
- Current version in code: `v1`
- Default auth: JWT bearer (`Authorization: Bearer <token>`)
- Refresh token transport: secure cookie (`CookieSettings` driven)
- Standard error style: ProblemDetails + domain error mapping
- Correlation header:
  - request/response: `X-Correlation-Id`
  - auto-generated if absent

## Endpoint Catalog

Legend:
- Auth: `Public`, `User`, `Admin`
- RL: applied named rate-limit policy

### Public Discovery APIs
| Method | Endpoint | Auth | RL | Description |
|---|---|---|---|---|
| GET | `/api/v1/home/featured-deals` | Public | `public-read` | Home featured deals (cached). |
| GET | `/api/v1/home/trending-cities` | Public | `public-read` | Top cities by recent visits (cached). |
| GET | `/api/v1/home/config` | Public | `public-read` | Search defaults and amenities list. |
| GET | `/api/v1/search` | Public | `public-read` | Hotel search with filters, sorting, cursor pagination. |
| GET | `/api/v1/hotels/{id}` | Public | `public-read` | Hotel details. |
| GET | `/api/v1/hotels/{id}/gallery` | Public | `public-read` | Hotel images/gallery. |
| GET | `/api/v1/hotels/{id}/room-availability` | Public | `public-read` | Room availability for date range. |
| GET | `/api/v1/hotels/{id}/reviews` | Public | `public-read` | Paginated reviews. |

### Auth APIs
| Method | Endpoint | Auth | RL | Description |
|---|---|---|---|---|
| POST | `/api/v1/auth/register` | Public | `auth` | Register user and issue token/cookie. |
| POST | `/api/v1/auth/login` | Public | `auth` | Login and issue token/cookie. |
| POST | `/api/v1/auth/refresh` | Public (cookie based) | `auth-refresh` | Rotate refresh token and return new JWT. |
| GET | `/api/v1/auth/profile` | User | `user-read` | Current user profile. |
| PUT | `/api/v1/auth/profile` | User | `user-write` | Update current user profile. |
| POST | `/api/v1/auth/change-password` | User | `user-write` | Change password. |
| POST | `/api/v1/auth/logout` | User | `user-write` | Logout current session family. |
| POST | `/api/v1/auth/logout-all` | User | `user-write` | Revoke all sessions for current user. |

### User Activity APIs
| Method | Endpoint | Auth | RL | Description |
|---|---|---|---|---|
| POST | `/api/v1/events/hotel-viewed` | User | `events` | Track hotel view event. |
| POST | `/api/v1/events/hotel-view` | User | `events` | Alias for hotel-viewed. |
| GET | `/api/v1/events/recently-visited` | User | `user-read` | Recently visited hotels for current user. |

### Cart APIs
| Method | Endpoint | Auth | RL | Description |
|---|---|---|---|---|
| GET | `/api/v1/cart` | User | `user-read` | Get current cart. |
| POST | `/api/v1/cart/items` | User | `user-write` | Add item to cart. |
| PUT | `/api/v1/cart/items/{itemId}` | User | `user-write` | Update item quantity. |
| DELETE | `/api/v1/cart/items/{itemId}` | User | `user-write` | Remove item. |
| DELETE | `/api/v1/cart` | User | `user-write` | Clear cart. |

### Checkout And Booking APIs
| Method | Endpoint | Auth | RL | Description |
|---|---|---|---|---|
| POST | `/api/v1/checkout/hold` | User | `checkout-hold` | Create checkout hold(s) from cart. |
| POST | `/api/v1/checkout/booking` | User | `checkout-booking` | Create pending booking + Stripe payment URL. |
| GET | `/api/v1/bookings` | User | `user-read` | List current user bookings. |
| GET | `/api/v1/bookings/{id}` | User/Admin (owner-or-admin) | `user-read` | Booking details. |
| POST | `/api/v1/bookings/{id}/cancel` | User/Admin (owner-or-admin) | `user-write` | Cancel booking + refund workflow. |

### Webhook And Health APIs
| Method | Endpoint | Auth | RL | Description |
|---|---|---|---|---|
| POST | `/api/v1/webhooks/stripe` | Public (signature validated) | `webhooks` | Stripe webhook endpoint (`256KB` max payload). |
| GET | `/api/v1/health/live` | Admin by default | Global only | Liveness probe (`Predicate = false`). |
| GET | `/api/v1/health/ready` | Admin by default | Global only | Readiness probe + SQL health check. |

Health endpoints become anonymous only when:
- `Monitoring__AllowAnonymousHealthEndpoints=true`

### Admin APIs

All admin APIs require role `Admin` and use admin-focused rate limiting.

#### Admin Cities (`/api/v1/admincities`)
| Method | Endpoint | RL | Description |
|---|---|---|---|
| GET | `/api/v1/admincities` | `admin` | Paginated city list. |
| POST | `/api/v1/admincities` | `admin` | Create city. |
| GET | `/api/v1/admincities/{id}` | `admin` | Get city by id. |
| PUT | `/api/v1/admincities/{id}` | `admin` | Update city. |
| DELETE | `/api/v1/admincities/{id}` | `admin` | Delete city. |

#### Admin Hotels (`/api/v1/adminhotels`)
| Method | Endpoint | RL | Description |
|---|---|---|---|
| GET | `/api/v1/adminhotels` | `admin` | Paginated hotels list. |
| POST | `/api/v1/adminhotels` | `admin` | Create hotel. |
| GET | `/api/v1/adminhotels/{id}` | `admin` | Get hotel by id. |
| PUT | `/api/v1/adminhotels/{id}` | `admin` | Update hotel. |
| DELETE | `/api/v1/adminhotels/{id}` | `admin` | Delete hotel. |
| POST | `/api/v1/adminhotels/{id}/images` | `admin-uploads` | Upload image (`multipart/form-data`, request limit `6MB`, validated image size limit default `5MB`). |
| DELETE | `/api/v1/adminhotels/{hotelId}/images/{imageId}` | `admin` | Delete hotel image metadata. |
| PUT | `/api/v1/adminhotels/{hotelId}/images/{imageId}` | `admin` | Update image caption/sort order. |
| PATCH | `/api/v1/adminhotels/{hotelId}/images/{imageId}/set-thumbnail` | `admin` | Set thumbnail image. |
| POST | `/api/v1/adminhotels/{hotelId}/services` | `admin` | Link service/amenity to hotel. |
| DELETE | `/api/v1/adminhotels/{hotelId}/services/{serviceId}` | `admin` | Unlink service from hotel. |

#### Admin Room Types (`/api/v1/adminroomtypes`)
| Method | Endpoint | RL | Description |
|---|---|---|---|
| GET | `/api/v1/adminroomtypes` | `admin` | Paginated room-type list. |
| POST | `/api/v1/adminroomtypes` | `admin` | Create room type. |
| GET | `/api/v1/adminroomtypes/{id}` | `admin` | Get room type by id. |
| PUT | `/api/v1/adminroomtypes/{id}` | `admin` | Update room type. |
| DELETE | `/api/v1/adminroomtypes/{id}` | `admin` | Delete room type. |

#### Admin Rooms (`/api/v1/adminrooms`)
| Method | Endpoint | RL | Description |
|---|---|---|---|
| GET | `/api/v1/adminrooms` | `admin` | Paginated rooms list (filterable). |
| POST | `/api/v1/adminrooms` | `admin` | Create room. |
| GET | `/api/v1/adminrooms/{id}` | `admin` | Get room by id. |
| PUT | `/api/v1/adminrooms/{id}` | `admin` | Update room. |
| DELETE | `/api/v1/adminrooms/{id}` | `admin` | Delete room. |

#### Admin Services (`/api/v1/adminservices`)
| Method | Endpoint | RL | Description |
|---|---|---|---|
| GET | `/api/v1/adminservices` | `admin` | Paginated services list. |
| POST | `/api/v1/adminservices` | `admin` | Create service. |
| GET | `/api/v1/adminservices/{id}` | `admin` | Get service by id. |
| PUT | `/api/v1/adminservices/{id}` | `admin` | Update service. |
| DELETE | `/api/v1/adminservices/{id}` | `admin` | Delete service. |

#### Admin Featured Deals (`/api/v1/adminfeatureddeals`)
| Method | Endpoint | RL | Description |
|---|---|---|---|
| GET | `/api/v1/adminfeatureddeals` | `admin` | Paginated featured deals list. |
| POST | `/api/v1/adminfeatureddeals` | `admin` | Create featured deal. |
| PUT | `/api/v1/adminfeatureddeals/{id}` | `admin` | Update featured deal. |
| DELETE | `/api/v1/adminfeatureddeals/{id}` | `admin` | Delete featured deal. |

#### Admin Payments (`/api/v1/adminpayments`)
| Method | Endpoint | RL | Description |
|---|---|---|---|
| GET | `/api/v1/adminpayments` | `admin` | Paginated payments list with filters. |

#### Admin Hotel-RoomType Mapping (`/api/v1/admin/hotel-room-types`)
| Method | Endpoint | RL | Description |
|---|---|---|---|
| GET | `/api/v1/admin/hotel-room-types` | `admin` | List mappings (optional `hotelId`). |
| POST | `/api/v1/admin/hotel-room-types` | `admin` | Create mapping with capacities and pricing. |
| GET | `/api/v1/admin/hotel-room-types/{id}` | `admin` | Get mapping by id. |
| PUT | `/api/v1/admin/hotel-room-types/{id}` | `admin` | Update mapping. |
| DELETE | `/api/v1/admin/hotel-room-types/{id}` | `admin` | Delete mapping. |

## Rate Limiting

The API has:
- a global token-bucket limiter (partitioned by client IP)
- per-endpoint named policies (partitioned by user-id or IP, depending on auth state)

Default values (`appsettings.json`):

| Policy | Algorithm | Default |
|---|---|---|
| Global | Token bucket | `300/min` |
| `auth` | Fixed window | `10/min` |
| `auth-refresh` | Fixed window | `20/min` |
| `public-read` | Token bucket | `240/min` |
| `user-read` | Token bucket | `180/min` |
| `user-write` | Fixed window | `60/min` |
| `checkout-hold` | Sliding window | `8/min`, `4` segments |
| `checkout-booking` | Fixed window | `12/min` |
| `events` | Fixed window | `120/min` |
| `admin` | Fixed window | `120/min` |
| `admin-uploads` | Fixed window | `100/min` |
| `webhooks` | Fixed window | `60/min` |

When limited, API returns:
- `429 Too Many Requests`
- JSON body: `{"error":"rate_limited","message":"Too many requests. Please retry later."}`
- `Retry-After` header when available

## Configuration

### Required Settings (startup-critical)
- `ConnectionStrings__DefaultConnection`
- `JWT__Secret` (>= 32 chars), `JWT__Issuer`, `JWT__Audience`
- `Stripe__SecretKey`
- `Stripe__WebhookSecret`
- `PaymentUrls__SuccessUrlTemplate` and `PaymentUrls__CancelUrlTemplate` (must be absolute URLs and include `{0}` placeholder)
- `Email__SmtpHost`, `Email__SmtpUser`, `Email__SmtpPassword`, `Email__FromAddress`

### Important Optional Settings
- `BookingSettings__CheckoutHoldMinutes`, `BookingSettings__TaxRate`, cancellation settings
- `RateLimiting__*` policies
- `Monitoring__AllowAnonymousHealthEndpoints`
- `Swagger__Enabled`
- `Cors__AllowedOrigins`
- `ForwardedHeaders__KnownProxies`, `ForwardedHeaders__KnownNetworks`
- `AdminBootstrap__*` (optional first-admin bootstrap)
- `Elasticsearch__*`, `Embedding__*`

### Docker Compose Variables

Compose currently expects:
- `SA_PASSWORD`
- `JWT_SECRET`
- `SEQ_ADMIN_PASSWORD`
- `STRIPE_SECRET_KEY`
- `STRIPE_WEBHOOK_SECRET`
- `ELASTIC_PASSWORD`

Note: `.env.example` includes most required values; add `ELASTIC_PASSWORD` before `docker compose up`.

## Run With Docker

1. Copy env template:
```bash
cp .env.example .env
```

2. Add missing compose variable:
```bash
# add to .env
ELASTIC_PASSWORD=ChangeMe_ElasticPass123!
```

3. Start services:
```bash
docker compose up -d --build
```

4. Main endpoints:
- API: `http://localhost:5009`
- Swagger (development): `http://localhost:5009/swagger`
- Seq: `http://localhost:5341`
- Elasticsearch: `http://localhost:9200`
- Kibana: `http://localhost:5601`
- Embedding service: `http://localhost:8000`

## Run Locally

1. Configure SQL Server connection in `appsettings.json` or environment variables.
2. Set required secrets (JWT/Stripe/Email/PaymentUrls).
3. Run API:
```bash
dotnet run --project src/HotelBooking.Api
```

Development startup behavior:
- applies pending EF Core migrations
- runs initial data seeding

Non-development startup behavior:
- validates DB connectivity
- does not auto-seed

## Seeding And Admin Bootstrap

### Data Seeder (Development)
On clean DB, seeder creates:
- city/service/room-type lookups
- hotels, hotel-room-type relations, rooms
- featured deals

### Optional Admin Bootstrap
To create/promote first admin at startup:
- `AdminBootstrap__Enabled=true`
- `AdminBootstrap__Email=admin@yourdomain.com`
- `AdminBootstrap__Password=StrongPass123!`
- optional name fields and email-confirmed behavior

If an admin already exists, bootstrap skips creation.

## Testing

Test projects:
- `tests/HotelBooking.Domain.Tests`
- `tests/HotelBooking.Application.Tests`
- `tests/HotelBooking.Api.IntegrationTests`

Run all:
```bash
dotnet test HotelBooking.sln -m:1 -v minimal
```

Run per test layer:
```bash
dotnet test tests/HotelBooking.Domain.Tests/HotelBooking.Domain.Tests.csproj -m:1 -v minimal
dotnet test tests/HotelBooking.Application.Tests/HotelBooking.Application.Tests.csproj -m:1 -v minimal
dotnet test tests/HotelBooking.Api.IntegrationTests/HotelBooking.Api.IntegrationTests.csproj -m:1 -v minimal
```

Integration tests use:
- Testcontainers for SQL Server
- fake payment gateway
- fake email service
- non-blocking rate-limit overrides for test stability

## CI

- Workflow: `.github/workflows/ci.yml`
- Triggers: push/PR on `dev` and `main`, plus manual dispatch
- Runs restore, build, all test projects under `tests/`, and coverage artifact generation

## Monitoring And Security

### Logging And Monitoring
- Serilog request logging + Seq sink
- Correlation IDs via `X-Correlation-Id`
- Health endpoints:
  - `/api/v1/health/live`
  - `/api/v1/health/ready`
- Monitoring runbook:
  - `docs/monitoring-alerts-runbook.md`

### Security Controls Implemented
- JWT strict validation (`ClockSkew = 0`)
- refresh token in `HttpOnly` cookie with configurable `Secure`/`SameSite`/`Path`
- refresh token hashing + rotation + family revocation on reuse
- security headers middleware:
  - `X-Content-Type-Options: nosniff`
  - `X-Frame-Options: DENY`
  - strict referrer and permissions policy
  - strict API CSP
- default `Cache-Control: no-store` for sensitive responses
- forwarded headers support for reverse proxy deployments (`X-Forwarded-For`, `X-Forwarded-Proto`)
- production startup secret validation (`JWT`, Stripe, Email password placeholders blocked)

## Useful Docs
- Monitoring runbook: `docs/monitoring-alerts-runbook.md`
- Compose stack: `docker-compose.yml`
- API startup and middleware: `src/HotelBooking.Api/Program.cs`, `src/HotelBooking.Api/DependencyInjection.cs`
- Integration test factory: `tests/HotelBooking.Api.IntegrationTests/Infrastructure/WebAppFactory.cs`

---

This repository is suitable as a graduation project backend and already includes realistic production concerns (auth/session security, payment/webhook safety, operational monitoring, and API hardening).
