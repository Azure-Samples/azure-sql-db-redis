targetScope = 'resourceGroup'

// Parameters
@description('Required. Azure location to which the resources are to be deployed')
param location string

@description('Required. Application name')
param applicationName string

@description('Optional. The tags to be assigned to the created resources.')
param tags object = {}

var resourceNames = {
  keyVault: 'kv-${applicationName}'
  applicationInsights: 'appi-${applicationName}'
  logAnalyticsWorkspace: 'log-${applicationName}'
  storageAccount: 'st${applicationName}'
}

// Resources
module appInsights './appi.bicep' = {
  name: 'appInsights-Deployment'
  params: {
    location: location
    name: resourceNames.applicationInsights
    logAnalyticsWorkspaceName: resourceNames.logAnalyticsWorkspace
    tags: tags
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: resourceNames.keyVault
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enabledForTemplateDeployment: true // ARM is permitted to retrieve secrets from the key vault. 
  }
}

//Create Storage Account
resource storageAccount 'Microsoft.Storage/storageAccounts@2022-05-01' = {
  name: resourceNames.storageAccount
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'Storage'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: true
    networkAcls: {
      bypass: 'AzureServices'
      virtualNetworkRules: []
      ipRules: []
      defaultAction: 'Allow'
    }
    supportsHttpsTrafficOnly: true
    encryption: {
      services: {
        file: {
          keyType: 'Account'
          enabled: true
        }
        blob: {
          keyType: 'Account'
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
  }
}

// Outputs
output appInsightsConnectionString string = appInsights.outputs.appInsightsConnectionString
output appInsightsInstrumentationKey string = appInsights.outputs.appInsightsInstrumentationKey
output keyVaultName string = keyVault.name
output storageAccountName string = storageAccount.name
