// Parameters
@description('Required. Azure location to which the resources are to be deployed')
param location string

param applicationName string

param anotherResourceGroup string

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

// Variables
var resourceNames = {
  redisLocationName1: '${applicationName}-reader-${location}'
  redisLocationName2: '${applicationName}-sweeper-${location}'
  redisDbName: 'default'
  redisGeoReplicationGroupName: 'gr-${applicationName}'
}


var rdbPersistence = persistenceOption == 'RDB' ? true : false
var aofPersistence = persistenceOption == 'AOF' ? true : false
var enableZoneRedundancy = availabilityZoneOption == true ? ['1','2','3'] : null

//Resources
resource redisLocation1 'Microsoft.Cache/redisEnterprise@2022-01-01' = {
  name: resourceNames.redisLocationName1
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

resource redisLocationDb1 'Microsoft.Cache/redisEnterprise/databases@2022-01-01' = {
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
          id: '/subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Cache/redisEnterprise/${resourceNames.redisLocationName1}/databases/default'
        }
       ]
    }
  }
}

resource redisLocation2 'Microsoft.Cache/redisEnterprise@2022-01-01' = {
  name: resourceNames.redisLocationName2
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

resource redisLocationDb2 'Microsoft.Cache/redisEnterprise/databases@2022-01-01' = {
  name: resourceNames.redisDbName
  parent: redisLocation2
  dependsOn:[
    redisLocation1
    redisLocationDb1
  ]
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
          id: '/subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Cache/redisEnterprise/${resourceNames.redisLocationName1}/databases/default'
        }
        {
          id: '/subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Cache/redisEnterprise/${resourceNames.redisLocationName2}/databases/default'
        }
       ]
    }
  }
}

module redisPassword '../core/security/keyvault-secret.bicep' = {
  name: 'redisPassword'
  scope: resourceGroup2
  params: {
    name: 'REDIS-ACCESS-KEY'
    keyVaultName: keyVault.name
    secretValue: redisLocationDb1.listKeys().primaryKey
  }
}

module redisConnection '../core/security/keyvault-secret.bicep' = {
  name: 'redisConnection'
  scope: resourceGroup2
  params: {
    name: 'REDIS-CONNECTION-KEY'
    keyVaultName: keyVault.name
    secretValue: format(redisConnectionFormat, redisLocation2.properties.hostName, redisLocationDb2.properties.port, redisLocationDb2.listKeys().primaryKey)
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2022-11-01' existing = {
  name: keyVaultName
  scope: resourceGroup2
}

resource resourceGroup2 'Microsoft.Resources/resourceGroups@2022-09-01' existing = {
  name: anotherResourceGroup
  scope: subscription()
}

var redisConnectionFormat = '{0}:{1},ssl=true,password={2},allowAdmin=true,syncTimeout=5000,connectTimeout=1000'

//Output
output redisLocationName1 string = redisLocation1.name
output redisLocationId1 string = redisLocation1.id
output redisLocationHostName1 string = redisLocation1.properties.hostName
output redisLocationHostPort1 string = '${redisLocationDb1.properties.port}'

output redisLocationName2 string = redisLocation2.name
output redisLocationId2 string = redisLocation2.id
output redisLocationHostName2 string = redisLocation2.properties.hostName
output redisLocationHostPort2 string = '${redisLocationDb2.properties.port}'

output redisAccessKey string = 'REDIS-ACCESS-KEY'
output redisConnectionkey string = 'REDIS-CONNECTION-KEY'
