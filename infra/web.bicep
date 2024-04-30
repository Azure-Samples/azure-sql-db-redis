@description('Web app name.')
@minLength(2)
param name string
@description('Location for all resources.')
param location string = resourceGroup().location
param tags object = {}
param containerAppsEnvironmentName string
param containerRegistryName string
param imageName string
param redisHost string
param redisPort string
param redisAccessKey string
param readThroughFunctionBaseUrl string
param keyVaultName string
param applicationInsightsConnectionString string
param anotherResourceGroup string

var serviceName = 'web'

module app 'core/host/container-app.bicep' = {
  name: '${serviceName}-container-app-module'
  params: {
    name: name
    location: location
    tags: union(tags, { 'azd-service-name': serviceName })
    containerAppsEnvironmentName: containerAppsEnvironmentName
    containerRegistryName: containerRegistryName
    containerCpuCoreCount: '1.0'
    containerMemory: '2.0Gi'
    env: [
      {
        name: 'REDIS_HOST'
        value: redisHost
      }
      {
        name: 'REDIS_PORT'
        value: redisPort
      }
      {
        name: 'REDIS_ACCESS_KEY'
        value: redisAccessKey
      }
      {
        name: 'IS_ACRE'
        value: 'true'
      }
      {
        name: 'ALLOW_ADMIN'
        value: 'true'
      }
      {
        name: 'DELETE_ALL_KEYS_ONLOAD'
        value: 'true'
      }
      {
        name: 'LOAD_INITIAL_DATA'
        value: 'true'
      }
      {
        name: 'USE_READ_THROUGH'
        value: 'false'
      }
      {
        name: 'USE_WRITE_BEHIND'
        value: 'true'
      }
      {
        name: 'READ_THROUGH_FUNCTION_BASE_URL'
        value: readThroughFunctionBaseUrl
      }
      {
        name: 'AZURE_KEY_VAULT_ENDPOINT'
        value: keyVault.properties.vaultUri
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        value: applicationInsightsConnectionString
      }
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Development'
      }
    ]
    imageName: !empty(imageName) ? imageName : 'nginx:latest'
    keyVaultName: keyVault.name
    anotherResourceGroup: anotherResourceGroup
  }
}

module keyVaultAccess 'core/security/keyvault-access.bicep' = {
  name: '${serviceName}-keyvault-access'
  scope: resourceGroup2
  params: {
    keyVaultName: keyVault.name
    principalId: app.outputs.identityPrincipalId
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
  name: keyVaultName
  scope: resourceGroup2
}

resource resourceGroup2 'Microsoft.Resources/resourceGroups@2022-09-01' existing = {
  name: anotherResourceGroup
  scope: subscription()
}

output SERVICE_WEB_IDENTITY_PRINCIPAL_ID string = app.outputs.identityPrincipalId
output SERVICE_WEB_NAME string = app.outputs.name
output SERVICE_WEB_URI string = app.outputs.uri
output SERVICE_WEB_IMAGE_NAME string = app.outputs.imageName
