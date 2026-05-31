# Infrastructure (Azure Container Apps target)

This folder contains Bicep templates to provision the minimum production stack:

- Azure Container Registry (ACR)
- Azure Container Apps Environment + Container App
- Azure SQL Server + Database
- Storage Account + Blob container + File share (DataProtection keys)
- Key Vault
- Log Analytics + Application Insights

## Prerequisites

- Azure CLI logged in (`az login`)
- Target subscription selected (`az account set --subscription <id>`)
- Permission to create role assignments in the target resource group

## Deploy

1. Create resource group:

```powershell
az group create -n <rg-name> -l <location>
```

2. Copy and edit parameters:

```powershell
Copy-Item infra/main.parameters.example.json infra/main.parameters.json
```

3. Deploy:

```powershell
az deployment group create `
  -g <rg-name> `
  -f infra/main.bicep `
  -p @infra/main.parameters.json
```

## Notes

- The template configures Container App to read critical runtime values from Key Vault using Managed Identity.
- DataProtection keys are persisted to an Azure Files mount at `/mnt/dpkeys`.
- If `identityRequireConfirmedAccount=true`, configure `smtpHost` and `smtpFrom` or app startup will fail by design.
