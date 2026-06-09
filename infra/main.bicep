targetScope = 'resourceGroup'

@description('Short project prefix for resource naming.')
param prefix string = 'hotelbooking'

@description('Deployment location.')
param location string = resourceGroup().location

@description('Environment label used in tags.')
param environmentName string = 'prod'

@description('Azure Container Registry name (must be globally unique, lowercase, 5-50 chars).')
param acrName string

@description('Container App environment name.')
param containerAppEnvironmentName string = '${prefix}-aca-env'

@description('Container App name.')
param containerAppName string = '${prefix}-web'

@description('Azure SQL logical server name (globally unique).')
param sqlServerName string

@description('Azure SQL database name.')
param sqlDatabaseName string = '${prefix}-db'

@description('SQL admin login.')
param sqlAdminLogin string = 'sqladminuser'

@secure()
@description('SQL admin password.')
param sqlAdminPassword string

@description('Storage account name (globally unique, lowercase, 3-24 chars).')
param storageAccountName string

@description('Blob container name for image storage.')
param imageContainerName string = 'hotel-images'

@description('Azure Managed Redis resource name.')
param redisName string = '${prefix}-redis'

@description('Azure Managed Redis SKU.')
param redisSkuName string = 'Balanced_B0'

@description('Azure Managed Redis database name.')
param redisDatabaseName string = 'default'

@description('Azure Managed Redis encrypted database port.')
param redisPort int = 10000

@description('Fail readiness checks when Redis is unavailable.')
param redisRequiredForReadiness bool = false

@description('Enable Redis-backed distributed rate limiting.')
param redisRateLimitingEnabled bool = true

@description('File share name used for DataProtection key persistence.')
param dataProtectionShareName string = 'dpkeys'

@description('Managed environment storage registration name.')
param dataProtectionStorageName string = 'dpkeysstorage'

@description('Key Vault name (globally unique).')
param keyVaultName string

@description('Log Analytics workspace name.')
param logAnalyticsWorkspaceName string = '${prefix}-law'

@description('Application Insights component name.')
param appInsightsName string = '${prefix}-appi'

@description('Container image repository path in ACR.')
param imageRepository string = 'hotelbooking'

@description('Container image tag to deploy.')
param imageTag string = 'latest'

@description('Container CPU allocation.')
param containerCpu int = 1

@description('Container memory allocation.')
param containerMemory string = '2Gi'

@description('Expose Container App ingress publicly.')
param ingressExternal bool = true

@description('Enable confirmed account requirement for Identity in Stage/Prod.')
param identityRequireConfirmedAccount bool = false

@description('SMTP host used when confirmed account is required.')
param smtpHost string = ''

@description('SMTP sender address used when confirmed account is required.')
param smtpFrom string = ''

@description('Allow public blob access for the image container.')
param allowBlobPublicAccess bool = true

var tags = {
  environment: environmentName
  workload: 'hotel-booking'
  managedBy: 'bicep'
}

var sqlConnectionString = 'Server=tcp:${sqlServerName}.database.windows.net,1433;Initial Catalog=${sqlDatabaseName};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
var storageAccountKey = listKeys(storage.id, storage.apiVersion).keys[0].value
var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storageAccountKey};EndpointSuffix=${environment().suffixes.storage}'
var publicBaseUrl = 'https://${storage.name}.blob.${environment().suffixes.storage}/${imageContainerName}'
var redisConnectionString = '${redisEnterprise.properties.hostName}:${redisDatabase.properties.port},password=${redisDatabase.listKeys().primaryKey},ssl=True,abortConnect=False'

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  sku: {
    name: 'Basic'
  }
  tags: tags
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  tags: tags
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  tags: tags
  properties: {
    allowBlobPublicAccess: allowBlobPublicAccess
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  name: '${storage.name}/default'
}

resource imageContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  name: '${storage.name}/default/${imageContainerName}'
  properties: {
    publicAccess: allowBlobPublicAccess ? 'Blob' : 'None'
  }
}

resource fileService 'Microsoft.Storage/storageAccounts/fileServices@2023-05-01' = {
  name: '${storage.name}/default'
}

resource dataProtectionShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-05-01' = {
  name: '${storage.name}/default/${dataProtectionShareName}'
  properties: {
    enabledProtocols: 'SMB'
    shareQuota: 5
  }
}

resource sqlServer 'Microsoft.Sql/servers@2022-11-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    publicNetworkAccess: 'Enabled'
    minimalTlsVersion: '1.2'
  }
}

resource sqlAllowAzureServices 'Microsoft.Sql/servers/firewallRules@2022-11-01-preview' = {
  name: '${sqlServer.name}/AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-11-01-preview' = {
  name: '${sqlServer.name}/${sqlDatabaseName}'
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    backupStorageRedundancy: 'Local'
    zoneRedundant: false
  }
}

resource redisEnterprise 'Microsoft.Cache/redisEnterprise@2025-04-01' = {
  name: redisName
  location: location
  tags: tags
  sku: {
    name: redisSkuName
  }
  properties: {
    encryption: {}
    highAvailability: 'Enabled'
    minimumTlsVersion: '1.2'
  }
}

resource redisDatabase 'Microsoft.Cache/redisEnterprise/databases@2025-04-01' = {
  name: redisDatabaseName
  parent: redisEnterprise
  properties: {
    clientProtocol: 'Encrypted'
    clusteringPolicy: 'OSSCluster'
    evictionPolicy: 'VolatileLRU'
    modules: []
    port: redisPort
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    tenantId: tenant().tenantId
    enableRbacAuthorization: true
    sku: {
      family: 'A'
      name: 'standard'
    }
    publicNetworkAccess: 'Enabled'
    enabledForDeployment: false
    enabledForTemplateDeployment: false
    enabledForDiskEncryption: false
  }
}

resource kvSqlConnection 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: '${keyVault.name}/ConnectionStrings--DefaultConnection'
  properties: {
    value: sqlConnectionString
  }
}

resource kvImageStorageConnection 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: '${keyVault.name}/ImageStorage--AzureBlob--ConnectionString'
  properties: {
    value: storageConnectionString
  }
}

resource kvImageStorageContainer 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: '${keyVault.name}/ImageStorage--AzureBlob--ContainerName'
  properties: {
    value: imageContainerName
  }
}

resource kvImageStoragePublicBaseUrl 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: '${keyVault.name}/ImageStorage--AzureBlob--PublicBaseUrl'
  properties: {
    value: publicBaseUrl
  }
}

resource kvRedisConnection 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: '${keyVault.name}/Redis--ConnectionString'
  properties: {
    value: redisConnectionString
  }
}

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: containerAppEnvironmentName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: workspace.properties.customerId
        sharedKey: listKeys(workspace.id, workspace.apiVersion).primarySharedKey
      }
    }
  }
}

resource containerAppStorage 'Microsoft.App/managedEnvironments/storages@2023-05-01' = {
  name: '${containerAppEnvironment.name}/${dataProtectionStorageName}'
  properties: {
    azureFile: {
      accountName: storage.name
      accountKey: storageAccountKey
      shareName: dataProtectionShareName
      accessMode: 'ReadWrite'
    }
  }
}

resource containerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: containerAppName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: ingressExternal
        targetPort: 8080
        transport: 'auto'
      }
      registries: [
        {
          server: acr.properties.loginServer
          identity: 'system'
        }
      ]
      secrets: [
        {
          name: 'default-connection'
          keyVaultUrl: kvSqlConnection.properties.secretUriWithVersion
          identity: 'system'
        }
        {
          name: 'image-storage-connection'
          keyVaultUrl: kvImageStorageConnection.properties.secretUriWithVersion
          identity: 'system'
        }
        {
          name: 'image-storage-container'
          keyVaultUrl: kvImageStorageContainer.properties.secretUriWithVersion
          identity: 'system'
        }
        {
          name: 'image-storage-public-base-url'
          keyVaultUrl: kvImageStoragePublicBaseUrl.properties.secretUriWithVersion
          identity: 'system'
        }
        {
          name: 'redis-connection'
          keyVaultUrl: kvRedisConnection.properties.secretUriWithVersion
          identity: 'system'
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'web'
          image: '${acr.properties.loginServer}/${imageRepository}:${imageTag}'
          resources: {
            cpu: containerCpu
            memory: containerMemory
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ConnectionStrings__DefaultConnection'
              secretRef: 'default-connection'
            }
            {
              name: 'ImageStorage__Provider'
              value: 'AzureBlob'
            }
            {
              name: 'ImageStorage__AzureBlob__ConnectionString'
              secretRef: 'image-storage-connection'
            }
            {
              name: 'ImageStorage__AzureBlob__ContainerName'
              secretRef: 'image-storage-container'
            }
            {
              name: 'ImageStorage__AzureBlob__PublicBaseUrl'
              secretRef: 'image-storage-public-base-url'
            }
            {
              name: 'DataProtection__PersistKeysToFileSystemPath'
              value: '/mnt/dpkeys'
            }
            {
              name: 'Redis__Enabled'
              value: 'true'
            }
            {
              name: 'Redis__ConnectionString'
              secretRef: 'redis-connection'
            }
            {
              name: 'Redis__InstanceName'
              value: 'HotelBooking:'
            }
            {
              name: 'Redis__RequiredForReadiness'
              value: string(redisRequiredForReadiness)
            }
            {
              name: 'Redis__RateLimiting__Enabled'
              value: string(redisRateLimitingEnabled)
            }
            {
              name: 'Azure__KeyVault__Uri'
              value: keyVault.properties.vaultUri
            }
            {
              name: 'Identity__RequireConfirmedAccount'
              value: string(identityRequireConfirmedAccount)
            }
            {
              name: 'Email__Smtp__Host'
              value: smtpHost
            }
            {
              name: 'Email__Smtp__From'
              value: smtpFrom
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appInsights.properties.ConnectionString
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health/live'
                port: 8080
              }
              initialDelaySeconds: 15
              periodSeconds: 10
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health/ready'
                port: 8080
              }
              initialDelaySeconds: 20
              periodSeconds: 10
            }
          ]
          volumeMounts: [
            {
              volumeName: 'dpkeys'
              mountPath: '/mnt/dpkeys'
            }
          ]
        }
      ]
      volumes: [
        {
          name: 'dpkeys'
          storageType: 'AzureFile'
          storageName: dataProtectionStorageName
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
}

resource acrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, containerApp.identity.principalId, 'acr-pull')
  scope: acr
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalId: containerApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource keyVaultSecretsUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, containerApp.identity.principalId, 'keyvault-secrets-user')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: containerApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output acrLoginServer string = acr.properties.loginServer
output containerAppFqdn string = containerApp.properties.configuration.ingress.fqdn
output containerAppIdentityPrincipalId string = containerApp.identity.principalId
output keyVaultUri string = keyVault.properties.vaultUri
