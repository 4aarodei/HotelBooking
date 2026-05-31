# Deployment Gap Baseline

Baseline date: 2026-05-31

## Current quality gate status

- `dotnet build HotelBooking.sln -c Release`: PASS
- `dotnet test HotelBooking.sln -c Release --no-build`: PASS
- `dotnet publish HotelBooking.Web -c Release --no-build`: PASS
- `docker build` / `docker run`: NOT VERIFIED in this workspace because Docker daemon was unavailable.

## Blockers closed in this iteration

- Added production-ready health split: `/health/live` and `/health/ready`.
- Added SQL retry policy for Azure SQL transient failures.
- Added reverse proxy handling via forwarded headers for container ingress.
- Added mandatory non-development DataProtection key persistence path policy.
- Added Azure Key Vault bootstrap support with `DefaultAzureCredential`.
- Added non-root container runtime configuration.
- Added IaC scaffolding for Azure Container Apps target stack.
- Added CI/CD deployment workflow with separate DB migration step.

## Remaining external prerequisites

- Azure subscription and permissions for:
  - Resource group deployment
  - Role assignments
  - Key Vault secret management
  - Container Apps deployment
- GitHub environment secrets/variables for Azure auth and deployment identifiers.
- Docker daemon available locally for container smoke tests.

## Readiness checklist

- [x] Runtime config contract documented for Stage/Prod
- [x] App fails fast on missing required production storage config
- [x] Health endpoints split for liveness/readiness
- [x] Migrations moved to separate deploy step in CD
- [x] Rollback path defined in deployment workflow
- [x] IaC template exists for ACA + ACR + SQL + Storage + Key Vault
- [ ] First full deployment executed in Azure and validated end-to-end
