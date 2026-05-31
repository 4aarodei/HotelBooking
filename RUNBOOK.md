# RUNBOOK: HotelBooking2 on Azure Container Apps

## 1. Deploy

1. Provision infra:
   - `az group create -n <rg> -l <location>`
   - `az deployment group create -g <rg> -f infra/main.bicep -p @infra/main.parameters.example.json`
2. Push app image:
   - CI/CD workflow `.github/workflows/deploy-aca.yml` builds and pushes to ACR.
3. Run DB migrations:
   - CD workflow runs `dotnet ef database update` as a separate step before app rollout.
4. Rollout app revision:
   - Workflow updates Container App image and waits for readiness.

## 2. Verify

- Health checks:
  - `https://<fqdn>/health/live`
  - `https://<fqdn>/health/ready`
- Smoke scenario:
  - open home page
  - search hotels
  - create booking
  - upload image and confirm Blob URL rendering

## 3. Rollback

If readiness smoke fails after deployment:

1. Identify previous revision:
   - `az containerapp revision list -g <rg> -n <app> -o table`
2. Reactivate prior stable revision:
   - `az containerapp revision activate -g <rg> -n <app> --revision <old-revision>`
3. Re-run health checks.

The `deploy-aca.yml` workflow already attempts this rollback automatically when smoke check fails.

## 4. Rotate secrets

1. Rotate secret in source system (SQL password, SMTP password, storage key).
2. Update Key Vault secret values.
3. Restart or roll a new Container App revision:
   - `az containerapp revision restart -g <rg> -n <app> --revision <active-revision>`
4. Validate `/health/ready`.

## 5. Incident recovery

### Database unreachable

- Expect `/health/ready` to fail while `/health/live` stays healthy.
- Validate SQL firewall/private access and credentials in Key Vault.

### Storage unreachable

- Check Blob service health and storage connection secret.
- Validate `ImageStorage__AzureBlob__*` values.

### Auth/session instability after restart

- Confirm `DataProtection__PersistKeysToFileSystemPath` points to mounted persistent volume.

## 6. Operational guardrails

- Never run EF migrations on app startup in Stage/Prod.
- Keep secrets out of repo and `appsettings`.
- Use Managed Identity for Key Vault access.
- Enforce readiness gate before shifting traffic to new revision.
