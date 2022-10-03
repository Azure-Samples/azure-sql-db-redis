// Parameters
@description('Required. Azure location to which the resources are to be deployed')
param location string

@description('Required. Azure secondary location to which the resources are to be deployed')
param location2 string

@description('Optional. The Azure Cache for Redis Enterprise sku.')
@allowed([
  'EnterpriseFlash_F1500'
  'EnterpriseFlash_F300'
  'EnterpriseFlash_F700'
  'Enterprise_E10'
  'Enterprise_E100'
  'Enterprise_E20'
  'Enterprise_E50'
])
param skuName string = 'Enterprise_E10'

@description('Optional. The Azure Cache for Redis Enterprise capacity.')
@allowed([
  2
  4
  6
  8
])
param capacity int = 2

@description('Optional. The Azure Cache for Redis Enterprise clustering policy.')
@allowed([
  'EnterpriseCluster'
  'OSSCluster'
])
param clusteringPolicy string = 'EnterpriseCluster'

@description('Optional. The Azure Cache for Redis Enteprise eviction policy.')
@allowed([
  'AllKeysLFU'
  'AllKeysLRU'
  'AllKeysRandom'
  'NoEviction'
  'VolatileLFU'
  'VolatileLRU'
  'VolatileRandom'
  'VolatileTTL'
])
param evictionPolicy string = 'NoEviction'

@description('Optional. Persist data stored in Azure Cache for Redis Enterprise.')
@allowed([
   'Disabled'
   'RDB'
   'AOF'
])
param persistenceOption string = 'Disabled'

@description('Optional. The frequency at which data is written to disk.')
@allowed([
  '1s'
  'always'
])
param aofFrequency string = '1s'

@description('Optional. The frequency at which a snapshot of the database is created.')
@allowed([
  '12h'
  '1h'
  '6h'
])
param rdbFrequency string = '6h'

@description('Optional. The Azure Cache for Redis Enterprise module(s)')
@allowed([
  'RedisBloom'
  'RedisTimeSeries'
  'RedisJSON'
  'RediSearch'
])
param modulesEnabled array = []

@description('Optional. The tags to be assigned to the created resources.')
param tags object = {}

@description('Required. Enable zone redundancy.')
param availabilityZoneOption bool = true

@description('Required. Key Vault Name')
param keyVaultName string

@description('Required. Application name')
param applicationName string

// Variables
var resourceNames = {
  redisLocation1Name: 'redis-${applicationName}-${location}'
  redisLocation2Name: 'redis-${applicationName}-${location2}'
  redisDbName: 'default'
  redisGeoReplicationGroupName: 'gr-${applicationName}'
}

var rdbPersistence = persistenceOption == 'RDB' ? true : false
var aofPersistence = persistenceOption == 'AOF' ? true : false
var enableZoneRedundancy = availabilityZoneOption == true ? ['1','2','3'] : null

//Resources
resource redisLocation1 'Microsoft.Cache/redisEnterprise@2022-01-01' = {
  name: resourceNames.redisLocation1Name
  location: location
  sku: {
    name: skuName
    capacity: capacity
  }
  properties: {
    minimumTlsVersion: '1.2'
  }
  zones: enableZoneRedundancy
  tags: tags
}

resource redisLocation1Db 'Microsoft.Cache/redisEnterprise/databases@2022-01-01' = {
  name: resourceNames.redisDbName
  parent: redisLocation1
  properties: {
    clientProtocol:'Encrypted'
    port: 10000
    clusteringPolicy: clusteringPolicy
    evictionPolicy: evictionPolicy
    persistence: {
      aofEnabled: aofPersistence
      aofFrequency: aofPersistence ? aofFrequency : null
      rdbEnabled: rdbPersistence
      rdbFrequency: rdbPersistence ? rdbFrequency : null
    }
    modules: [for module in modulesEnabled: {
      name: module
    }]
    geoReplication: {
       groupNickname: resourceNames.redisGeoReplicationGroupName 
       linkedDatabases: [
        {
          id: '/subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Cache/redisEnterprise/${resourceNames.redisLocation1Name}/databases/default'
        }
       ]
    }
  }
}

resource redisLocation2 'Microsoft.Cache/redisEnterprise@2022-01-01' = {
  name: resourceNames.redisLocation2Name
  location: location2
  dependsOn: [
    redisLocation1
  ]
  sku: {
    name: skuName
    capacity: capacity
  }
  properties: {
    minimumTlsVersion: '1.2'
  }
  zones: enableZoneRedundancy
  tags: tags
}

resource redisLocation2Db 'Microsoft.Cache/redisEnterprise/databases@2022-01-01' = {
  name: resourceNames.redisDbName
  parent: redisLocation2
  properties: {
    clientProtocol:'Encrypted'
    port: 10000
    clusteringPolicy: clusteringPolicy
    evictionPolicy: evictionPolicy
    persistence: {
      aofEnabled: aofPersistence
      aofFrequency: aofPersistence ? aofFrequency : null
      rdbEnabled: rdbPersistence
      rdbFrequency: rdbPersistence ? rdbFrequency : null
    }
    modules: [for module in modulesEnabled: {
      name: module
    }]
    geoReplication: {
       groupNickname: resourceNames.redisGeoReplicationGroupName
       linkedDatabases: [
          {
            id: '/subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Cache/redisEnterprise/${resourceNames.redisLocation1Name}/databases/default'
          }
          {
            id: '/subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Cache/redisEnterprise/${resourceNames.redisLocation2Name}/databases/default'
          }
        ]       
    }
  }
}


//Add endpoint to Key Vault
resource redisHostNameSecret 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
  name: '${keyVaultName}/redisHostName'  // The first part is KV's name
  properties: {
    value: redisLocation1.properties.hostName
  }
}

resource redisPassword 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
 name: '${keyVaultName}/redisPassword'  // The first part is KV's name
 properties: {
  value: '${listKeys(redisLocation1.id, redisLocation1.apiVersion).keys[0].value}'
 }
}

//Output
output redisLocation1Name string = redisLocation1.name
output redisLocation1Id string = redisLocation1.id
output redisLocation1HostName string = redisLocation1.properties.hostName

output redisLocation2Name string = redisLocation2.name
output redisLocation2Id string  = redisLocation2.name
output redisLocation2HostName string = redisLocation2.properties.hostName


