# HotelBooking2

ASP.NET Core MVC application for hotel search and booking, prepared for Docker + Azure Container Apps production deployment.

## Architecture

- `HotelBooking.Web` - MVC/UI, Identity, runtime composition.
- `HotelBooking.Application` - business logic, booking orchestration, media validation pipeline.
- `HotelBooking.Infrastructure` - EF Core, repositories, Dapper read-model, Azure Blob image storage adapter.
- `HotelBooking.Core` - domain entities and enums.

## Production readiness highlights

- Docker-ready multi-stage build with non-root runtime user.
- Fail-fast startup policy for non-development environments:
  - required SQL connection
  - required Azure Blob image storage configuration
  - required DataProtection key persistence path
  - SMTP required only when confirmed accounts are enforced
- Health endpoints split:
  - `/health/live` - process liveness
  - `/health/ready` - readiness including DB connectivity
  - `/health` - alias to readiness
- SQL retry policy enabled for Azure SQL transient errors.
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

Conditional:

- If `Identity__RequireConfirmedAccount=true`, then SMTP must be configured:
  - `Email__Smtp__Host`
  - `Email__Smtp__From`
  - optional auth settings (`Port`, `EnableSsl`, `UserName`, `Password`)

Optional:

- `Azure__KeyVault__Uri` (loads config from Key Vault via Managed Identity)
- `Sql__MaxRetryCount` (default `5`)
- `Sql__MaxRetryDelaySeconds` (default `10`)

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

1. Provision infra with Bicep.
2. Configure GitHub Azure OIDC secrets and deployment variables.
3. Run deploy workflow.
4. Verify `/health/live` and `/health/ready`.
