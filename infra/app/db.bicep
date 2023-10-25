param name string
@description('Location for all resources.')
param location string = resourceGroup().location
param tags object = {}
param keyValutName string
param databaseName string
@secure()
param sqlAdminPassword string
@secure()
param appUserPassword string
param storageAccountName string
param anotherResourceGroup string

module sql '../core/database/sqlserver/sqlserver.bicep' = {
  name: '${name}-sql-module'
  params: {
    name: name
    location: location
    databaseName: databaseName
    keyVaultName: keyValutName
    sqlAdminPassword: sqlAdminPassword
    appUserPassword: appUserPassword
    storageAccountName: storageAccountName
    tags: tags
    anotherResourceGroup: anotherResourceGroup
  }
}

output dbConnectionStringKey string = sql.outputs.connectionStringKey
