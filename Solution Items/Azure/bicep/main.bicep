targetScope = 'subscription'

@description('Optional. Azure main location to which the resources are to be deployed -defaults to the location of the current deployment')
param location string = deployment().location

@description('Optional. Azure second location to which the resources are to be deployed -defaults to west')
param location2 string 

@description('Optional. The tags to be assigned to the created resources.')
param tags object = {}

var applicationName = 'leaderboard'

var defaultTags = union({
  application: applicationName
}, tags)

var appResourceGroupName = 'rg-${applicationName}'
var sharedResourceGroupName = 'rg-shared-${applicationName}'

// Create resource groups
resource appResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: appResourceGroupName
  location: location
  tags: defaultTags
}

resource sharedResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: sharedResourceGroupName
  location: location
  tags: defaultTags
}

// Create shared resources
module shared './shared/shared.bicep' = {
  name: 'sharedresources-Deployment'
  scope: resourceGroup(sharedResourceGroup.name)
  params: {
    location: location
    applicationName: applicationName
    tags: defaultTags
  }
}

//Create App Service resources
module leaderboardApp 'app.bicep' = {
  scope: resourceGroup(appResourceGroup.name)
  name: 'appService-Deployment'
  params: {
    location: location
    location2: location2
    tags: tags
    applicationName: applicationName
    instrumentationKey: shared.outputs.appInsightsInstrumentationKey
  }
}

//Create SQL Resource
module sql 'sql.bicep' = {
  scope: resourceGroup(appResourceGroup.name)
  name: 'sql-Deployment'
  params: {
    location: location
    tags: tags
    applicationName: applicationName
  }
}

//Create Redis resource
module redis 'redis.bicep' = {
  dependsOn: [
    leaderboardApp
  ]
  scope: resourceGroup(appResourceGroup.name)
  name: 'redis-Deployment'
  params: {
    location: location
    location2: location2
    tags: defaultTags
    keyVaultName: shared.outputs.keyVaultName
    applicationName: applicationName
  }
}

//Create Function Apps
module functionApps 'func.bicep' = {
  dependsOn: [
   sql
   redis 
  ]
  scope: resourceGroup(appResourceGroup.name)
  name: 'functionApps-Deployment'
  params: {
    location: location
    tags: tags
    applicationName: applicationName
  }
}

//Create Front Door
module frontDoor 'frontDoor.bicep' = {
  dependsOn: [
    leaderboardApp
  ]
  scope: resourceGroup(appResourceGroup.name)
  name: 'frontDoor-Deployment'
  params: {
    application1Location: location
    application2Location: location2
    appHostName: leaderboardApp.outputs.appHostName
    app2HostName: leaderboardApp.outputs.app2HostName
    applicationName: applicationName
    tags: tags
  }
}

output appResourceGroupName string = appResourceGroup.name
output sharedResourceGroupName string = sharedResourceGroup.name
