# HotelBooking

A web application for searching hotels and creating bookings, built with ASP.NET Core MVC and EF Core.

## Current architecture (after refactor)

- **HotelBooking.Web**
  - MVC controllers, Razor views, view models, Identity UI.
- **HotelBooking.Application**
  - Application services with real orchestration (`BookingService`, `HotelService`).
  - Contracts for repositories and statistics queries.
  - Domain-level booking rule exception (`BookingRuleViolationException`).
- **HotelBooking.Infrastructure**
  - EF Core `ApplicationDbContext`.
  - Repository implementations.
  - Dapper read model for booking statistics.
- **HotelBooking.Core**
  - Domain entities and booking status enum.

## Key design decisions

1. **Removed thin proxy services**
   - `RoomService` and `BookingStatusService` were removed because they only forwarded repository calls.

2. **Kept only meaningful application orchestration**
   - `BookingService` now owns booking rules: date validation, availability check, nights/total calculation, and initial status assignment.
   - `HotelService` keeps availability orchestration for hotel search/details.

3. **Simplified booking status model**
   - Replaced duplicated identity status model (`StatusId` + external status code GUIDs) with a single `BookingStatus` enum on `Booking`.

4. **Improved date/time modeling**
   - Booking stay range uses `DateOnly` (`CheckIn`, `CheckOut`).
   - Creation timestamp uses `DateTimeOffset` (`CreatedAtUtc`).

5. **Safer null handling**
   - Replaced multiple `null!` property initializations in domain entities with `required` or nullable navigation references where appropriate.

6. **Exception handling for booking rules**
   - Introduced `BookingRuleViolationException` and mapped it in MVC controller handling.

7. **Automated booking rule tests**
   - Added `HotelBooking.Tests` with unit tests for `BookingService` booking rules.

## Booking rules covered in tests

- check-out must be later than check-in
- room must exist
- room must be active
- room capacity must not be exceeded for overlapping dates
- successful booking must set pending status, nights, and total price correctly

## Run locally

### Prerequisites

- .NET 8 SDK
- SQL Server / LocalDB

### Setup

```bash
git clone https://github.com/4aarodei/HotelBooking.git
cd HotelBooking
```

Set connection string (example):

```bash
cd HotelBooking.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=(localdb)\\mssqllocaldb;Database=HotelBooking;Trusted_Connection=True;MultipleActiveResultSets=true"
```

Apply migrations:

```bash
dotnet ef database update \
  --project HotelBooking.Infrastructure \
  --startup-project HotelBooking.Web
```

Run app:

```bash
dotnet run --project HotelBooking.Web
```

## Test command

```bash
dotnet test HotelBooking.Tests/HotelBooking.Tests.csproj
```
