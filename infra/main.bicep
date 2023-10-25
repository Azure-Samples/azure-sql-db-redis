targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the the environment which is used to generate a short unique hash used in all resources.')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string = 'eastus'

@description('Id of the user or app to assign application roles')
param principalId string = ''

@secure()
@description('SQL Server administrator password')
param sqlAdminPassword string

@secure()
@description('Application user password')
param appUserPassword string

param webImageName string

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = { 'azd-env-name': environmentName }

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: '${abbrs.resourcesResourceGroups}leadboard-${environmentName}'
  location: location
  tags: tags
}

resource rg2 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: '${abbrs.resourcesResourceGroups}shard-leadboard-${environmentName}'
  location: location
  tags: tags
}

// Container apps host (including container registry)
module containerApps './core/host/container-apps.bicep' = {
  name: 'container.apps'
  scope: rg2
  params: {
    name: 'app'
    containerAppsEnvironmentName: '${abbrs.appManagedEnvironments}${resourceToken}'
    containerRegistryName: '${abbrs.containerRegistryRegistries}${resourceToken}'
    location: location
    logAnalyticsWorkspaceName: monitoring.outputs.logAnalyticsWorkspaceName
  }
}

// Monitor application with Azure Monitor
module monitoring 'core/monitor/monitoring.bicep' = {
  name: 'monitoring'
  scope: rg2
  params: {
    location: location
    tags: tags
    logAnalyticsName: '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    applicationInsightsName: '${abbrs.insightsComponents}${resourceToken}'
    applicationInsightsDashboardName: '${abbrs.portalDashboards}${resourceToken}'
  }
}

module appServicePlan 'core/host/appserviceplan.bicep' = {
  name: 'appserviceplan'
  scope: rg2
  params: {
    name: '${abbrs.webServerFarms}${resourceToken}'
    location: location
    tags: tags
    sku: {name: 'B1'}
  }
}

// Store secrets in a keyvault
module keyVault 'core/security/keyvault.bicep' = {
  name: 'keyvault'
  scope: rg2
  params: {
    name: '${abbrs.keyVaultVaults}${resourceToken}'
    location: location
    tags: tags
    principalId: principalId
  }
}

module storage 'app/storage.bicep' = {
  name: 'storage'
  scope: rg2
  params: {
    name: '${abbrs.storageStorageAccounts}${resourceToken}'
    location: location
    allowBlobPublicAccess: true
    keyVaultName: keyVault.outputs.name
    tags: tags
  }
  dependsOn:[
    keyVault
  ]
}

module sqlServer 'app/db.bicep' = {
  name: 'sql'
  scope: rg
  params: {
    name: '${abbrs.sqlServers}${resourceToken}'
    location: location
    databaseName: '${abbrs.sqlServersDatabases}${resourceToken}'
    keyValutName: keyVault.outputs.name
    sqlAdminPassword: sqlAdminPassword
    appUserPassword: appUserPassword
    storageAccountName: storage.outputs.name
    tags: tags
    anotherResourceGroup: rg2.name
  }
  dependsOn: [
    keyVault
  ]
}

module redis 'app/redis.bicep' = {
  name: 'redis'
  scope: rg
  params: {
    applicationName: '${abbrs.cacheRedis}${resourceToken}'
    keyVaultName: keyVault.outputs.name
    location: location
    anotherResourceGroup: rg2.name
  }
  dependsOn:[
    keyVault
  ]
}

module reader 'core/host/functions.bicep' = {
  name: 'reader'
  scope: rg
  params: {
    name: '${abbrs.webSitesFunctions}reader-${resourceToken}'
    location: location
    tags: union(tags, {'azd-service-name': 'reader'})
    alwaysOn: true
    appServicePlanId: appServicePlan.outputs.id
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    runtimeName: 'dotnet-isolated'
    runtimeVersion: '6.0'
    storageAccountName: storage.outputs.name
    keyVaultName: keyVault.outputs.name
    scmDoBuildDuringDeployment: false
    appSettings: {
      REDIS_HOST: redis.outputs.redisLocationHostName1
      REDIS_PORT: redis.outputs.redisLocationHostPort1
      REDIS_ACCESS_KEY: redis.outputs.redisAccessKey
      IS_ACRE: 'true'
      AZURE_SQL_CONNECTION_STRING_KEY: sqlServer.outputs.dbConnectionStringKey
    }
    anotherResourceGroup: rg2.name
  }
  dependsOn:[
    appServicePlan
    monitoring
    storage
    keyVault
    redis
    sqlServer
  ]
}

module sweeper 'core/host/functions.bicep' = {
  name: 'sweeper'
  scope: rg
  params: {
    name: '${abbrs.webSitesFunctions}sweeper-${resourceToken}'
    location: location
    tags: union(tags, {'azd-service-name': 'sweeper'})
    alwaysOn: true
    appServicePlanId: appServicePlan.outputs.id
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    runtimeName: 'dotnet'
    runtimeVersion: '6.0'
    storageAccountName: storage.outputs.name
    keyVaultName: keyVault.outputs.name
    scmDoBuildDuringDeployment: false
    appSettings: {
      AZURE_SQL_CONNECTION_STRING_KEY: sqlServer.outputs.dbConnectionStringKey
      REDIS_CONNECTION_STRING_KEY: redis.outputs.redisConnectionkey
      TimerInterval: '0 */1 * * * *'
    }
    anotherResourceGroup: rg2.name
  }
  dependsOn:[
    appServicePlan
    monitoring
    storage
    keyVault
    redis
    sqlServer
  ]
}

module readerKeyVaultAccess 'core/security/keyvault-access.bicep' = {
  name: 'reader-keyvault-access'
  scope: rg2
  params: {
    keyVaultName: keyVault.outputs.name
    principalId: reader.outputs.identityPrincipalId
  }
  dependsOn:[
    keyVault
    reader
  ]
}

module sweeperKeyVaultAccess 'core/security/keyvault-access.bicep' = {
  name: 'sweeper-keyvault-access'
  scope: rg2
  params: {
    keyVaultName: keyVault.outputs.name
    principalId: sweeper.outputs.identityPrincipalId
  }
  dependsOn:[
    keyVault
    sweeper
  ]
}

module web 'web.bicep' = {
  name: 'web'
  scope: rg
  params: {
    name: '${abbrs.appContainerApps}web-${resourceToken}'
    location: location
    containerAppsEnvironmentName: containerApps.outputs.environmentName
    containerRegistryName: containerApps.outputs.registryName
    imageName: webImageName
    keyVaultName: keyVault.outputs.name
    tags: tags
    redisHost: redis.outputs.redisLocationHostName1
    redisPort: redis.outputs.redisLocationHostPort1
    redisAccessKey: redis.outputs.redisAccessKey
    readThroughFunctionBaseUrl: reader.outputs.uri
    applicationInsightsConnectionString: monitoring.outputs.applicationInsightsConnectionString
    anotherResourceGroup: rg2.name
  }
  dependsOn:[
    containerApps
    keyVault
    redis
    reader
  ]
}

module webConfig 'app/webConfig.bicep' = {
  name: 'webConfig'
  scope: rg
  params: {
    apiName: reader.outputs.name
    properties: {
      cors: {
        allowedOrigins: [
          web.outputs.SERVICE_WEB_URI
        ]
      }
    }
  }
  dependsOn:[
    reader
    web
  ]
}

output APPLICATIONINSIGHTS_CONNECTION_STRING string = monitoring.outputs.applicationInsightsConnectionString
output APPLICATIONINSIGHTS_NAME string = monitoring.outputs.applicationInsightsName
output AZURE_ANOTHER_RESOURCE_GROUP string = rg2.name
output AZURE_CONTAINER_ENVIRONMENT_NAME string = containerApps.outputs.environmentName
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = containerApps.outputs.registryLoginServer
output AZURE_CONTAINER_REGISTRY_NAME string = containerApps.outputs.registryName
output AZURE_KEY_VAULT_ENDPOINT string = keyVault.outputs.endpoint
output AZURE_KEY_VAULT_NAME string = keyVault.outputs.name
output AZURE_LOCATION string = location
output AZURE_RESOURCE_GROUP string = rg.name
output AZURE_TENANT_ID string = tenant().tenantId
output REACT_READ_THROUGH_FUNCTION_BASE_URL string = reader.outputs.uri
output REACT_REDIS_ACCESS_KEY string = redis.outputs.redisAccessKey
output REACT_REDIS_HOST string = redis.outputs.redisLocationHostName1
output REACT_REDIS_PORT string = redis.outputs.redisLocationHostPort1
output REACT_SQL_CONNECTION_STRING_KEY string = sqlServer.outputs.dbConnectionStringKey
output SERVICE_WEB_NAME string = web.outputs.SERVICE_WEB_NAME
