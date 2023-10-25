param name string
param location string = resourceGroup().location
param tags object = {}

param allowBlobPublicAccess bool = false
param containers array = []
param kind string = 'StorageV2'
param minimumTlsVersion string = 'TLS1_2'
param sku object = { name: 'Standard_LRS' }
param keyVaultName string

resource storage 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: name
  location: location
  tags: tags
  kind: kind
  sku: sku
  properties: {
    minimumTlsVersion: minimumTlsVersion
    allowBlobPublicAccess: allowBlobPublicAccess
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
  }

  resource blobServices 'blobServices' = if (!empty(containers)) {
    name: 'default'
    resource container 'containers' = [for container in containers: {
      name: container.name
      properties: {
        publicAccess: contains(container, 'publicAccess') ? container.publicAccess : 'None'
      }
    }]
  }
}

resource storagePrimaryKeySecret 'Microsoft.KeyVault/vaults/secrets@2022-11-01' = {
  parent: keyVault
  name: 'STORAGE-PRIMARY-KEY'
  properties: {
    value: storagePrimaryKey
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2022-11-01' existing = {
  name: keyVaultName
}

var storagePrimaryKey = storage.listKeys().keys[0].value

output name string = storage.name
output id string = storage.id
output primaryEndpoints object = storage.properties.primaryEndpoints
output storagePrimaryKeyConnectionStringKey string = 'STORAGE-PRIMARY-KEY'
