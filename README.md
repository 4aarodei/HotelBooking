# HotelBooking

A web application for searching hotels and creating bookings, built with ASP.NET Core MVC and EF Core.

## Current architecture (after refactor)

- **HotelBooking.Web**
  - MVC controllers, Razor views, view models, Identity UI.
- **HotelBooking.Application**
  - Application services with real orchestration (`BookingService`, `HotelService`).
  - Contracts for repositories and statistics queries.
  - Media contracts and image validation/re-encoding pipeline.
  - Domain-level booking rule exception (`BookingRuleViolationException`).
- **HotelBooking.Infrastructure**
  - EF Core `ApplicationDbContext`.
  - Repository implementations.
  - Dapper read model for booking statistics.
  - Azure Blob Storage image adapter.
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

8. **Production-style media storage**
   - Image storage is abstracted behind `IImageStorage`.
   - Production can use Azure Blob Storage via `ImageStorage:Provider=AzureBlob`.
   - Local disk storage remains only as the development fallback.
   - Image records store storage key, public URL, content type, size, dimensions, and creation time.
   - Uploads are checked by extension, declared content type, file signature, real image decode, dimensions, pixel count, and size, then re-encoded to WebP with metadata stripped.
   - `Room.ImageUrl` has been removed from the domain model; room images are now sourced from `RoomImages`.

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

Optional development admin seed:

```bash
dotnet user-secrets set "AdminSeed:Email" "admin@hotelbooking.local"
dotnet user-secrets set "AdminSeed:Password" "Use-a-strong-local-password-123!"
```

If `AdminSeed:Password` is not configured, the roles are created but the default SuperAdmin user is skipped. This keeps secrets out of source control.

Email confirmation is disabled by default in Development and enabled by default outside Development. Override it when needed:

```bash
dotnet user-secrets set "Identity:RequireConfirmedAccount" "false"
```

For environments where confirmed accounts are enabled, configure SMTP:

```bash
dotnet user-secrets set "Email:Smtp:Host" "smtp.example.com"
dotnet user-secrets set "Email:Smtp:Port" "587"
dotnet user-secrets set "Email:Smtp:EnableSsl" "true"
dotnet user-secrets set "Email:Smtp:UserName" "smtp-user"
dotnet user-secrets set "Email:Smtp:Password" "smtp-password"
dotnet user-secrets set "Email:Smtp:From" "no-reply@example.com"
```

Development image uploads use local disk by default:

```bash
dotnet user-secrets set "ImageStorage:Provider" "Local"
```

Production and staging must use Azure Blob Storage and environment variables or managed secret storage:

```bash
ImageStorage__Provider=AzureBlob
ImageStorage__AzureBlob__ConnectionString=<azure-storage-connection-string>
ImageStorage__AzureBlob__ContainerName=hotel-images
ImageStorage__AzureBlob__PublicBaseUrl=https://<cdn-or-storage-host>/hotel-images
ConnectionStrings__DefaultConnection=<azure-sql-connection-string>
```

The app exposes `/health` for container and platform health probes. Do not rely on `wwwroot/uploads` as persistent production storage; the Docker image ignores uploaded files and expects durable media to live in Blob Storage.

Outside `Development`, the app refuses to start unless:

```bash
ImageStorage__Provider=AzureBlob
ImageStorage__AzureBlob__ConnectionString=<azure-storage-connection-string>
ImageStorage__AzureBlob__ContainerName=<container-name>
ImageStorage__AzureBlob__PublicBaseUrl=https://<cdn-or-storage-host>/<container-name>
```

This prevents accidental production deployment with local file storage.

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

## Docker

Build the production image:

```bash
docker build -t hotelbooking .
```

Run with environment-based configuration:

```bash
docker run --rm -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="<connection-string>" \
  -e ImageStorage__Provider=AzureBlob \
  -e ImageStorage__AzureBlob__ConnectionString="<storage-connection-string>" \
  -e ImageStorage__AzureBlob__ContainerName="hotel-images" \
  -e ImageStorage__AzureBlob__PublicBaseUrl="https://<cdn-or-storage-host>/hotel-images" \
  hotelbooking
```

## Environment model

- `Development`
  - Runs with `dotnet run` as the primary workflow.
  - May use LocalDB and `ImageStorage=Local`.
  - Applies migrations and development seed data on startup.
- `Staging`
  - Intended for future Docker/Azure Container Apps deployment.
  - Must use Azure SQL and `ImageStorage=AzureBlob`.
  - Must receive secrets from environment variables or managed secret storage.
- `Production`
  - Intended for Docker + Azure Container Apps.
  - Must use Azure SQL and `ImageStorage=AzureBlob`.
  - Must not execute automatic DB migrations in web startup.

## Azure Container Apps target

Planned production deployment model:

- Docker image built in CI
- Image pushed to Azure Container Registry
- Azure Container App updated to the new image revision
- Azure SQL used as the application database
- Azure Blob Storage used for hotel and room images
- Optional next steps: Key Vault and Application Insights

## Pre-Docker readiness checklist

- `dotnet build` passes
- `dotnet test` passes
- `dotnet publish` passes
- Production startup fails fast when Blob Storage is not configured
- No production image flow depends on `wwwroot/uploads`
- Public/admin views render room and hotel images from image metadata
- Production DB migrations are planned as a separate deployment step

## CI

The repository includes a GitHub Actions workflow at `.github/workflows/ci.yml` that restores, builds, and tests the solution on Windows with .NET 8.
