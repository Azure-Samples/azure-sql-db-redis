

@description('Required. Main location')
param location string

@description('Optional. The tags to be assigned to the created resources.')
param tags object = {}

@description('Optional. Administrator username')
param adminUserName string = 'sql-admin'

@description('Required. Application name')
param applicationName string


var resourceNames = {
  sqlServerName: 'sql-${applicationName}-${location}'
  sqlServerDbName: 'sqldb-${applicationName}-${location}'
}


resource sqlServer 'Microsoft.Sql/servers@2022-02-01-preview' = {
  name: resourceNames.sqlServerName
  location: location
  tags: tags
  properties: {
    administratorLogin: adminUserName
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    restrictOutboundNetworkAccess: 'Disabled'
  }
}

resource sqlServerDb 'Microsoft.Sql/servers/databases@2022-02-01-preview' = {
  parent: sqlServer
  name: resourceNames.sqlServerDbName
  location: location
  tags: tags
  sku: {
    name: 'Standard'
    tier: 'Standard'
    capacity: 10
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 268435456000
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
    readScale: 'Disabled'
    requestedBackupStorageRedundancy: 'Local'
    isLedgerOn: false
  }
}

output sqlServerName string = sqlServer.name
output sqlServerDbName string = sqlServerDb.name
