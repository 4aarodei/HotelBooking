# HotelBooking2

ASP.NET Core MVC application for hotel search and booking, prepared for Docker + Azure Container Apps production deployment.

## Architecture

- `HotelBooking.Web` - MVC/UI, Identity screens, health endpoints, and runtime composition.
- `HotelBooking.Application` - use cases/queries, repository/cache/media ports, booking orchestration, and media validation pipeline.
- `HotelBooking.Infrastructure` - EF Core, ASP.NET Identity user storage, repositories, Dapper read-model, Redis adapters, and Azure Blob image storage.
- `HotelBooking.Domain` - domain entities, value objects, enums, factories, and stable invariants with no ASP.NET Identity/EF dependency.

Important boundaries:

- Domain does not reference ASP.NET Core Identity or EF Core.
- `ApplicationUser` lives in Infrastructure; bookings keep a stable `UserId`.
- Web controllers call Application services/use cases rather than repositories.
- Architecture guard tests protect the main layer rules.

## Production readiness highlights

- Docker-ready multi-stage build with non-root runtime user.
- Fail-fast startup policy for non-development environments:
  - required SQL connection
  - required Azure Blob image storage configuration
  - required DataProtection key persistence path
  - SMTP required only when confirmed accounts are enforced
- Health endpoints split:
  - `/health/live` - process liveness
  - `/health/ready` - readiness including DB connectivity, plus Redis only when explicitly required
  - `/health` - alias to readiness
- SQL retry policy enabled for Azure SQL transient errors.
- Redis-ready distributed cache and fixed-window rate limiting with fail-open behavior for public browsing and booking attempts.
- Reverse proxy support for ingress environments (Azure Container Apps).
- Key Vault configuration provider support via `DefaultAzureCredential`.

## Local development

Prerequisites:

- .NET 8 SDK
- SQL Server / LocalDB

Run:

```powershell
dotnet restore HotelBooking.sln
dotnet build HotelBooking.sln -c Release
dotnet test HotelBooking.sln -c Release
dotnet run --project HotelBooking.Web
```

## Local container run (production profile)

Build image:

```powershell
docker build -f HotelBooking.Web/docker/Dockerfile -t hotelbooking .
```

Run:

```powershell
docker compose -f HotelBooking.Web/docker/docker-compose.yml up --build
```

The compose profile starts SQL Server, Redis `7-alpine`, and the web app. Redis is enabled there with:

- `Redis__Enabled=true`
- `Redis__ConnectionString=redis:6379`
- `Redis__InstanceName=HotelBooking:`
- `Redis__RateLimiting__Enabled=true`

Or run the app image directly:

```powershell
docker run --rm -p 8080:8080 `
  -e ASPNETCORE_ENVIRONMENT=Production `
  -e ConnectionStrings__DefaultConnection="<azure-sql-connection-string>" `
  -e ImageStorage__Provider=AzureBlob `
  -e ImageStorage__AzureBlob__ConnectionString="<storage-connection-string>" `
  -e ImageStorage__AzureBlob__ContainerName="hotel-images" `
  -e ImageStorage__AzureBlob__PublicBaseUrl="https://<storage-account>.blob.core.windows.net/hotel-images" `
  -e DataProtection__PersistKeysToFileSystemPath="/app/dpkeys" `
  -e Redis__Enabled="true" `
  -e Redis__ConnectionString="<redis-host>:10000,password=<key>,ssl=True,abortConnect=False" `
  -e Redis__InstanceName="HotelBooking:" `
  -e Redis__RateLimiting__Enabled="true" `
  -e Identity__RequireConfirmedAccount="false" `
  hotelbooking
```

## Runtime configuration contract (Stage/Prod)

Required:

- `ConnectionStrings__DefaultConnection`
- `ImageStorage__Provider=AzureBlob`
- `ImageStorage__AzureBlob__ConnectionString`
- `ImageStorage__AzureBlob__ContainerName`
- `ImageStorage__AzureBlob__PublicBaseUrl`
- `DataProtection__PersistKeysToFileSystemPath`

Redis:

- `Redis__Enabled` (default `false`)
- `Redis__ConnectionString` (required when Redis is enabled)
- `Redis__InstanceName` (default `HotelBooking:`)
- `Redis__RequiredForReadiness` (default `false`)
- `Redis__RateLimiting__Enabled` (requires Redis)

Conditional:

- If `Identity__RequireConfirmedAccount=true`, then SMTP must be configured:
  - `Email__Smtp__Host`
  - `Email__Smtp__From`
  - optional auth settings (`Port`, `EnableSsl`, `UserName`, `Password`)

Optional:

- `Azure__KeyVault__Uri` (loads config from Key Vault via Managed Identity)
- `ReverseProxy__KnownProxies__0` / `ReverseProxy__KnownNetworks__0` (trusted ingress IPs/CIDRs for `X-Forwarded-*`; otherwise only ASP.NET Core defaults are trusted)
- `Sql__MaxRetryCount` (default `5`)
- `Sql__MaxRetryDelaySeconds` (default `10`)

Redis caches stable read snapshots only:

- cities list for 12 hours
- featured hotels for 15 minutes
- hotel search results for 60 seconds, keyed by both catalog and availability versions

Booking creation, final availability validation, user profile/bookings, and admin mutation pages stay SQL-backed. If Redis is unavailable at runtime, cache and rate limiting fail open and log warnings; SQL remains the source of truth for bookings.

## Database migrations policy

- Development: app applies migrations on startup.
- Stage/Production: migrations are executed as a **separate deployment step** (not app startup).

Command:

```powershell
dotnet ef database update `
  --project HotelBooking.Infrastructure/HotelBooking.Infrastructure.csproj `
  --startup-project HotelBooking.Web/HotelBooking.Web.csproj
```

## Azure deployment

- IaC: `infra/main.bicep`
- CD workflow: `.github/workflows/deploy-aca.yml`
- Operational runbook: `RUNBOOK.md`
- Environment matrix: `ENVIRONMENT_MATRIX.md`
- Baseline gap log: `DEPLOYMENT_GAP.md`

Typical flow:

1. Provision infra with Bicep, including Azure Managed Redis.
2. Configure GitHub Azure OIDC secrets and deployment variables.
3. Run deploy workflow.
4. Verify `/health/live` and `/health/ready`.
