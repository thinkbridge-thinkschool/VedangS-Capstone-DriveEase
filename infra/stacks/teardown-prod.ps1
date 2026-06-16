<#
.SYNOPSIS
  Tears down the DriveEase PROD Deployment Stack and ALL managed resources.

.DESCRIPTION
  Because the stack was created with --action-on-unmanage deleteAll,
  deleting the stack deletes every resource it deployed (App Service, SQL,
  Service Bus) — no orphaned resources left behind.

  Double-confirmation required for production safety.

.EXAMPLE
  .\infra\stacks\teardown-prod.ps1
#>

$ErrorActionPreference = 'Stop'

$stackName     = 'driveease-prod-stack'
$resourceGroup = 'rg-driveease-prod'

Write-Host ''
Write-Host '=====================================================' -ForegroundColor Red
Write-Host '  DriveEase — PROD Teardown  *** PRODUCTION ***' -ForegroundColor Red
Write-Host "  Stack : $stackName" -ForegroundColor Red
Write-Host "  RG    : $resourceGroup" -ForegroundColor Red
Write-Host '  This will DELETE all PROD resources permanently.' -ForegroundColor Red
Write-Host '=====================================================' -ForegroundColor Red

$confirm1 = Read-Host 'Type the stack name to confirm'
if ($confirm1 -ne $stackName) {
  Write-Host 'Stack name mismatch. Teardown cancelled.' -ForegroundColor Yellow
  exit 0
}

$confirm2 = Read-Host 'Type DELETE-PROD to proceed'
if ($confirm2 -ne 'DELETE-PROD') {
  Write-Host 'Confirmation failed. Teardown cancelled.' -ForegroundColor Yellow
  exit 0
}

Write-Host ''
Write-Host 'Deleting deployment stack (and all managed resources)...' -ForegroundColor Yellow
az stack group delete `
  --name                $stackName `
  --resource-group      $resourceGroup `
  --action-on-unmanage  deleteAll `
  --yes

Write-Host ''
Write-Host 'Deleting resource group...' -ForegroundColor Yellow
az group delete --name $resourceGroup --yes --no-wait

Write-Host ''
Write-Host '=====================================================' -ForegroundColor Green
Write-Host '  PROD environment fully cleaned up.' -ForegroundColor Green
Write-Host '=====================================================' -ForegroundColor Green
