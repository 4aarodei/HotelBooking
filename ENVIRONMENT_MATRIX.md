# Environment Matrix

## Development

- `ASPNETCORE_ENVIRONMENT=Development`
- Database: LocalDB or local SQL Server
- Image storage: `ImageStorage__Provider=Local`
- DataProtection key persistence: optional
- Migrations: automatic on startup (enabled by app code)
- Identity confirmed account: disabled by default unless explicitly enabled

## Staging

- `ASPNETCORE_ENVIRONMENT=Staging`
- Database: Azure SQL
- Image storage: Azure Blob only
- DataProtection keys: required external path (mounted volume recommended)
- Migrations: separate pipeline step, never app startup
- Reverse proxy: enabled (Container Apps ingress)
- Secrets source: Key Vault + Managed Identity

## Production

- `ASPNETCORE_ENVIRONMENT=Production`
- Database: Azure SQL
- Image storage: Azure Blob only
- DataProtection keys: required external path (mounted volume)
- Migrations: separate pipeline step, never app startup
- Reverse proxy: enabled
- Secrets source: Key Vault + Managed Identity
- Identity confirmed account:
  - `true` only if SMTP is configured
  - otherwise set `Identity__RequireConfirmedAccount=false`

## Required Stage/Prod env vars

- `ConnectionStrings__DefaultConnection`
- `ImageStorage__Provider=AzureBlob`
- `ImageStorage__AzureBlob__ConnectionString`
- `ImageStorage__AzureBlob__ContainerName`
- `ImageStorage__AzureBlob__PublicBaseUrl`
- `DataProtection__PersistKeysToFileSystemPath`
- `Identity__RequireConfirmedAccount` (`false` if SMTP is not configured)

## Optional Stage/Prod env vars

- `Azure__KeyVault__Uri` (enables Key Vault configuration provider)
- `Email__Smtp__Host`
- `Email__Smtp__Port`
- `Email__Smtp__EnableSsl`
- `Email__Smtp__UserName`
- `Email__Smtp__Password`
- `Email__Smtp__From`
- `Sql__MaxRetryCount`
- `Sql__MaxRetryDelaySeconds`
