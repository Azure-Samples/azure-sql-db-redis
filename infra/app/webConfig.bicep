param properties object = {}
param apiName string

resource apiSite 'Microsoft.Web/sites@2022-03-01' existing = {
  name: apiName
}

resource webConfig 'Microsoft.Web/sites/config@2022-03-01' = {
  name: 'web'
  parent: apiSite
  properties: properties
}
