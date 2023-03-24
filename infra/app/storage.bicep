param name string
param location string = resourceGroup().location
param allowBlobPublicAccess bool = true
param keyVaultName string
param tags object = {}

var serviceName = 'storage'

module storage '../core/storage/storage-account.bicep' = {
  name: '${name}-storage-module'
  params: {
    name: name
    location: location
    tags: union(tags, { 'azd-service-name': serviceName })
    allowBlobPublicAccess: allowBlobPublicAccess
    keyVaultName: keyVaultName
  }
}

output name string = storage.outputs.name
output id string = storage.outputs.id
output primaryEndpoints object = storage.outputs.primaryEndpoints
output storagePrimaryKeyConnectionStringKey string = storage.outputs.storagePrimaryKeyConnectionStringKey
