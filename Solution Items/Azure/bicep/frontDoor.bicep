@description('The name of the Front Door endpoint to create. This must be globally unique.')
param frontDoorEndpointName string = 'afd-${uniqueString(resourceGroup().id)}'

@description('Optional. The tags to be assigned to the created resources.')
param tags object = {}

@description('Required. Application name')
param applicationName string

@description('Required. App 1 location')
param application1Location string

@description('Required. Application 2 location')
param application2Location string

@description('Required. Application host name for location 1')
param appHostName string

@description('Required. Application host name for location 2')
param app2HostName string

var resourceNames = {
  frontDoorName : 'fd-${applicationName}'
  frontDoorOriginGroupName: 'default-origin-group'
  frontDoorOriginName: '${applicationName}-${application1Location}'
  frontDoorOrigin2Name: '${applicationName}-${application2Location}'
  frontDoorRouteName: 'default-route'
}

resource frontDoorProfile 'Microsoft.Cdn/profiles@2021-06-01' = {
  name: resourceNames.frontDoorName
  location: 'global'
  sku: {
    name: 'Standard_AzureFrontDoor'
  }
  tags: tags
}

resource frontDoorEndpoint 'Microsoft.Cdn/profiles/afdEndpoints@2021-06-01' = {
  name: frontDoorEndpointName
  parent: frontDoorProfile
  location: 'global'
  properties: {
    enabledState: 'Enabled'
  }
}

resource frontDoorOriginGroup 'Microsoft.Cdn/profiles/originGroups@2021-06-01' = {
  name: resourceNames.frontDoorOriginGroupName
  parent: frontDoorProfile
  properties: {
    loadBalancingSettings: {
      sampleSize: 4
      successfulSamplesRequired: 3
    }
    sessionAffinityState: 'Disabled'
    healthProbeSettings: {
      probePath: '/'
      probeRequestType: 'HEAD'
      probeProtocol: 'Http'
      probeIntervalInSeconds: 100
    }
  }
}

resource frontDoorOrigin 'Microsoft.Cdn/profiles/originGroups/origins@2021-06-01' = {
  name: resourceNames.frontDoorOriginName
  parent: frontDoorOriginGroup
  properties: {
    hostName: appHostName
    httpPort: 80
    httpsPort: 443
    originHostHeader: appHostName
    priority: 1
    weight: 1000
  }
}

resource frontDoorOrigin2 'Microsoft.Cdn/profiles/originGroups/origins@2021-06-01' = {
  name: resourceNames.frontDoorOrigin2Name
  parent: frontDoorOriginGroup
  properties: {
    hostName: app2HostName
    httpPort: 80
    httpsPort: 443
    originHostHeader: app2HostName
    priority: 1
    weight: 1000
  }
}

resource frontDoorRoute 'Microsoft.Cdn/profiles/afdEndpoints/routes@2021-06-01' = {
  name: resourceNames.frontDoorRouteName
  parent: frontDoorEndpoint
  dependsOn: [
    frontDoorOrigin // This explicit dependency is required to ensure that the origin group is not empty when the route is created.
  ]
  properties: {
    originGroup: {
      id: frontDoorOriginGroup.id
    }
    supportedProtocols: [
      'Http'
      'Https'
    ]
    patternsToMatch: [
      '/*'
    ]
    forwardingProtocol: 'HttpsOnly'
    linkToDefaultDomain: 'Enabled'
    httpsRedirect: 'Enabled'
  }
}

output frontDoorEndpointHostName string = frontDoorEndpoint.properties.hostName
