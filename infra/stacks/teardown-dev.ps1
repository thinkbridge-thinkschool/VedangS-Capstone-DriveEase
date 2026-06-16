<#
.SYNOPSIS
  Tears down the DriveEase DEV Deployment Stack and ALL managed resources.

.DESCRIPTION
  Because the stack was created with --action-on-unmanage deleteAll,
  deleting the stack deletes every resource it deployed (App Service, SQL,
  Service Bus) — no orphaned resources left behind.

.EXAMPLE
  .\infra\stacks\teardown-dev.ps1
#>

$ErrorActionPreference = 'Stop'

$stackName     = 'driveease-dev-stack'
$resourceGroup = 'rg-driveease-dev'

Write-Host ''
Write-Host '=====================================================' -ForegroundColor Red
Write-Host '  DriveEase — DEV Teardown' -ForegroundColor Red
Write-Host "  Stack : $stackName" -ForegroundColor Red
Write-Host "  RG    : $resourceGroup" -ForegroundColor Red
Write-Host '  This will DELETE all DEV resources.' -ForegroundColor Red
Write-Host '=====================================================' -ForegroundColor Red

$confirm = Read-Host 'Type YES to confirm teardown'
if ($confirm -ne 'YES') {
  Write-Host 'Teardown cancelled.' -ForegroundColor Yellow
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
Write-Host '  DEV environment fully cleaned up.' -ForegroundColor Green
Write-Host '=====================================================' -ForegroundColor Green
