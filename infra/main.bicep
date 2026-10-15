targetScope = 'subscription'

@minLength(1)
@maxLength(40)
@description('Environment name. Azure Developer CLI sets this from AZURE_ENV_NAME.')
param environmentName string

@description('Azure region. Azure Developer CLI sets this from AZURE_LOCATION.')
param location string

@description('SQL administrator login.')
param sqlAdminLogin string = 'northlineadmin'

@secure()
param sqlAdminPassword string

@secure()
@minLength(32)
param jwtSigningKey string

var tags = {
  'azd-env-name': environmentName
  project: 'northline-inventory-demo'
}

resource resourceGroup 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module app 'resources.bicep' = {
  name: 'northline'
  scope: resourceGroup
  params: {
    location: location
    namePrefix: 'nl${uniqueString(resourceGroup.id)}'
    tags: tags
    sqlAdminLogin: sqlAdminLogin
    sqlAdminPassword: sqlAdminPassword
    jwtSigningKey: jwtSigningKey
  }
}

output AZURE_LOCATION string = location
output AZURE_RESOURCE_GROUP string = resourceGroup.name
output API_URL string = app.outputs.apiUrl
output WEB_URL string = app.outputs.webUrl
output SQL_SERVER string = app.outputs.sqlServerFqdn
