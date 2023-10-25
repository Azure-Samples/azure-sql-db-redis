param name string
param location string = resourceGroup().location
param tags object = {}

param appUser string = 'appUser'
param databaseName string
param keyVaultName string
param sqlAdmin string = 'sqlAdmin'
param storageAccountName string

@secure()
param sqlAdminPassword string
@secure()
param appUserPassword string
param anotherResourceGroup string

resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: name
  location: location
  tags: tags
  properties: {
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administratorLogin: sqlAdmin
    administratorLoginPassword: sqlAdminPassword
  }

  resource database 'databases' = {
    name: databaseName
    location: location
  }

  resource firewall 'firewallRules' = {
    name: 'Azure Services'
    properties: {
      // Allow all clients
      // Note: range [0.0.0.0-0.0.0.0] means "allow all Azure-hosted clients only".
      // This is not sufficient, because we also want to allow direct access from developer machine, for debugging purposes.
      startIpAddress: '0.0.0.1'
      endIpAddress: '255.255.255.254'
    }
  }
}

resource sqlDeploymentScript 'Microsoft.Resources/deploymentScripts@2020-10-01' = {
  name: '${name}-deployment-script'
  location: location
  kind: 'AzureCLI'
  properties: {
    azCliVersion: '2.37.0'
    retentionInterval: 'PT1H' // Retain the script resource for 1 hour after it ends running
    timeout: 'PT5M' // Five minutes
    cleanupPreference: 'OnSuccess'
    storageAccountSettings: {
      storageAccountKey: storageAccount.listKeys().keys[0].value
      storageAccountName: storageAccount.name
    }
    environmentVariables: [
      {
        name: 'APPUSERNAME'
        value: appUser
      }
      {
        name: 'APPUSERPASSWORD'
        secureValue: appUserPassword
      }
      {
        name: 'DBNAME'
        value: databaseName
      }
      {
        name: 'DBSERVER'
        value: sqlServer.properties.fullyQualifiedDomainName
      }
      {
        name: 'SQLCMDPASSWORD'
        secureValue: sqlAdminPassword
      }
      {
        name: 'SQLADMIN'
        value: sqlAdmin
      }
    ]

    scriptContent: '''
wget https://github.com/microsoft/go-sqlcmd/releases/download/v0.8.1/sqlcmd-v0.8.1-linux-x64.tar.bz2
tar x -f sqlcmd-v0.8.1-linux-x64.tar.bz2 -C .

cat <<SCRIPT_END > ./initDb.sql
drop user ${APPUSERNAME}
go
create user ${APPUSERNAME} with password = '${APPUSERPASSWORD}'
go
alter role db_owner add member ${APPUSERNAME}
go
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[company](
	[symbol] [nvarchar](25) NOT NULL,
	[company] [nvarchar](100) NULL,
	[country] [nvarchar](50) NULL,
	[rank] [int] NULL,
	[marketcap] [float] NULL
) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
ALTER TABLE [dbo].[company] ADD PRIMARY KEY CLUSTERED 
(
	[symbol] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SCRIPT_END

./sqlcmd -S ${DBSERVER} -d ${DBNAME} -U ${SQLADMIN} -i ./initDb.sql
    '''
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' existing = {
  name: storageAccountName
  scope: resourceGroup2
}

module sqlAdminPasswordSecret '../../security/keyvault-secret.bicep' = {
  name: 'sqlAdminPasswordSecret'
  scope: resourceGroup2
  params: {
    keyVaultName: keyVault.name
    name: 'sqlAdminPassword'
    secretValue: sqlAdminPassword
  }
}

module appUserPasswordSecret '../../security/keyvault-secret.bicep' = {
  name: 'appUserPasswordSecret'
  scope: resourceGroup2
  params: {
    keyVaultName: keyVault.name
    name: 'appUserPassword'
    secretValue: appUserPassword
  }
}

module sqlAzureConnectionStringSercret '../../security/keyvault-secret.bicep' = {
  name: 'sqlAzureConnectionStringSercret'
  scope: resourceGroup2
  params: {
    keyVaultName: keyVault.name
    name: 'AZURE-SQL-CONNECTION-STRING'
    secretValue: connectionString
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

var connectionString = 'Server=tcp:${name}${environment().suffixes.sqlServerHostname},1433;Initial Catalog=${sqlServer::database.name};Persist Security Info=False;User ID=${appUser};Password=${appUserPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
output connectionStringKey string = 'AZURE-SQL-CONNECTION-STRING'
output databaseName string = sqlServer::database.name
