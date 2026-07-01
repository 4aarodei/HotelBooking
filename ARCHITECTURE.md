# HotelBooking Architecture

HotelBooking uses a layered architecture with explicit boundaries between the
domain model, application use cases, infrastructure adapters, and MVC
presentation.

Preferred flow:

```text
Web controller -> Application use case/query -> Domain rules -> Application port -> Infrastructure adapter
```

## Layers

### Domain

`HotelBooking.Domain` owns stable business concepts and invariants:

- `Hotel`, `Room`, `Booking`, images, booking status, and date range.
- Domain factories such as `Hotel.Create`, `Room.Create`, and `Booking.Create`.
- Domain validation that is independent of UI, persistence, cache, or storage.

Domain must not reference ASP.NET Core, EF Core, Identity, logging,
configuration, dependency injection, HTTP, SQL, Redis, or storage SDKs.

### Application

`HotelBooking.Application` owns orchestration and use cases:

- Booking use cases such as create booking and get user bookings.
- Public hotel queries such as cities, featured hotels, hotel search, hotel
  availability details, and room availability details.
- Admin command/query contracts and services.
- Ports for repositories, cache, media storage, statistics, rate limiting, and
  clock access.

Application may depend on Domain. It must not depend on Infrastructure or Web.
It is responsible for loading entities, coordinating repositories, cache
version bumps, image lifecycle orchestration, and translating domain violations
into application exceptions.

### Infrastructure

`HotelBooking.Infrastructure` implements external adapters:

- EF Core `ApplicationDbContext`, migrations, and repositories.
- ASP.NET Identity user type and identity seeding.
- Dapper statistics query.
- Redis cache/rate-limit adapters.
- Azure Blob image storage.
- Demo data seeding.

Infrastructure may depend on Application and Domain.

### Web

`HotelBooking.Web` owns presentation and composition:

- Controllers, Razor views, view models, model binding, authorization, redirects,
  `ModelState`, and `TempData`.
- DI/startup wiring for Application and Infrastructure.
- Identity UI and HTTP health endpoints.

Controllers should call Application services/use cases instead of repositories.
`ApplicationDbContext` usage in Web is limited to composition, Identity stores,
startup migration wiring, and health checks.

## Dependency Rules

Target project direction:

```text
Domain         -> no project dependencies
Application    -> Domain
Infrastructure -> Application + Domain
Web            -> Application + Infrastructure for composition
Tests          -> projects under test
```

Current pragmatic exception: Web still references Domain for public hotel view
model mapping. Business writes and repository access stay outside Web.

## Identity Boundary

`ApplicationUser` lives in `HotelBooking.Infrastructure.Identity` and extends
ASP.NET Core `IdentityUser`. Domain does not know about Identity users.

Bookings keep the stable `UserId` string. The `Booking -> ApplicationUser`
navigation has been removed from Domain; Identity-specific storage and user
management remain Infrastructure/Web concerns.

Authorization role constants live in `HotelBooking.Application.Security.AppRoles`
so Web authorization, registration, and Infrastructure seeding can share names
without placing policy in Domain.

## Application Feature Structure

- `Admin`: admin hotel/room commands, queries, read models, and image lifecycle.
- `Bookings`: booking use cases, booking service facade, and booking exceptions.
- `Hotels`: public hotel queries, hotel service facade, availability models, and
  cache read snapshots.
- `Persistence`: repository ports implemented by Infrastructure.
- `Caching`: cache port, Redis options, and cache keys.
- `Media`: image processing/storage abstractions and upload models.
- `Statistics`: statistics read models and query port.
- `Common`: shared application services such as clock access.
- `RateLimiting`: rate-limit abstraction used by Web workflows.

## Guard Tests

Architecture tests protect the key boundaries:

- Domain must not reference ASP.NET Core, EF Core, or Identity assemblies.
- Application must not reference Infrastructure or Web.
- Web controllers must not depend on repository interfaces.
