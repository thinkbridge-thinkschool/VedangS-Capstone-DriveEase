targetScope = 'resourceGroup'

// ─────────────────────────────────────────────────────────────────────────────
// DriveEase — main orchestration template
//
// Deployed via Azure Deployment Stacks (not plain `az deployment group create`).
// Use the scripts in infra/stacks/ — they call `az stack group create` which
// tracks every resource, detects drift, and enables clean one-command teardown.
//
// DEV stack  : .\infra\stacks\deploy-dev.ps1
// PROD stack : .\infra\stacks\deploy-prod.ps1
// App code   : azd deploy --environment <dev|prod>   (after stack is up)
// Teardown   : .\infra\stacks\teardown-dev.ps1
//
// What-if (plain preview, no stack):
//   az deployment group what-if -g rg-driveease-dev --template-file infra/main.bicep --parameters infra/parameters/dev.bicepparam
// ─────────────────────────────────────────────────────────────────────────────

@description('Environment name — drives SKU selection and resource naming')
@allowed(['dev', 'prod'])
param environmentName string

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('SQL Server administrator login')
param sqlAdminLogin string = 'sqladmin'

@secure()
@description('SQL Server administrator password')
param sqlAdminPassword string

// Stable 8-char suffix derived from subscription + RG — identical on every re-deploy
var suffix = take(uniqueString(subscription().id, resourceGroup().name), 8)
var prefix  = 'driveease-${environmentName}'

// ── SQL ───────────────────────────────────────────────────────────────────────
module sql 'modules/sql.bicep' = {
  name: 'deploy-sql'
  params: {
    serverName:   '${prefix}-sql-${suffix}'
    databaseName: 'driveease'
    location:      location
    adminLogin:    sqlAdminLogin
    adminPassword: sqlAdminPassword
    skuName:      environmentName == 'prod' ? 'S2'       : 'Basic'
    skuTier:      environmentName == 'prod' ? 'Standard' : 'Basic'
    skuCapacity:  environmentName == 'prod' ? 50         : 5
  }
}

// ── Service Bus ───────────────────────────────────────────────────────────────
// Standard SKU required — Basic has no topics (only queues),
// and DriveEase uses pub/sub for 4 async event flows.
module serviceBus 'modules/servicebus.bicep' = {
  name: 'deploy-servicebus'
  params: {
    namespaceName: '${prefix}-sb-${suffix}'
    location:       location
    skuName:       'Standard'
  }
}

// ── API ───────────────────────────────────────────────────────────────────────
module api 'modules/api.bicep' = {
  name: 'deploy-api'
  params: {
    appName:                    '${prefix}-api-${suffix}'
    location:                    location
    planSku:                    environmentName == 'prod' ? 'S1'   : 'B1'
    sqlConnectionString:        sql.outputs.connectionString
    serviceBusConnectionString: serviceBus.outputs.primaryConnectionString
    environmentName:             environmentName
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
output apiUrl              string = api.outputs.appUrl
output sqlServerFqdn       string = sql.outputs.serverFqdn
output serviceBusNamespace string = serviceBus.outputs.namespaceName
