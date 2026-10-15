@description('Short name used in Azure resource names.')
@minLength(3)
@maxLength(20)
param appName string = 'stockroom'

@description('Azure region. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('Administrator login for the Azure SQL server.')
param sqlAdminLogin string

@secure()
@description('Administrator password for the Azure SQL server.')
param sqlAdminPassword string

@secure()
@description('JWT signing key. Use at least 32 UTF-8 bytes and do not reuse the demo key.')
param jwtSigningKey string

var suffix = uniqueString(resourceGroup().id)
var sqlServerName = toLower(take('${appName}-sql-${suffix}', 60))
var databaseName = 'stockroom'
var planName = '${appName}-plan'
var webAppName = toLower(take('${appName}-api-${suffix}', 60))

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  kind: 'linux'
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  properties: {
    reserved: true
  }
  tags: {
    project: 'stockroom'
    purpose: 'portfolio-demo'
  }
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01' = {
  name: sqlServerName
  location: location
  tags: {
    project: 'stockroom'
    purpose: 'portfolio-demo'
  }
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource allowAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource database 'Microsoft.Sql/servers/databases@2023-08-01' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      alwaysOn: true
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'DatabaseProvider'
          value: 'SqlServer'
        }
        {
          name: 'ConnectionStrings__SqlServer'
          value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${databaseName};User ID=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
        }
        {
          name: 'Jwt__Issuer'
          value: 'stockroom'
        }
        {
          name: 'Jwt__Audience'
          value: 'stockroom'
        }
        {
          name: 'Jwt__Key'
          value: jwtSigningKey
        }
        {
          name: 'Jwt__ExpiryMinutes'
          value: '480'
        }
      ]
    }
  }
  tags: {
    project: 'stockroom'
    purpose: 'portfolio-demo'
  }
  dependsOn: [
    allowAzure
    database
  ]
}

output webAppName string = webApp.name
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
