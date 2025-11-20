# Deploy to Web Apps for Containers
# This script deploys the latest container images to Azure Web Apps

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("staging", "production")]
    [string]$Environment,
    
    [Parameter(Mandatory=$false)]
    [string]$ImageTag = "latest",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipHealthCheck
)

# Set environment-specific variables
switch ($Environment) {
    "staging" {
        $ResourceGroup = "rg-amlink-submissions-mcp-staging"
        $ClientAppName = "app-amlink-submissions-mcp-staging-client"
        $ServerAppName = "app-amlink-submissions-mcp-staging-server"
        $Location = "eastus2"
    }
    "production" {
        $ResourceGroup = "rg-amlink-submissions-mcp-prod"
        $ClientAppName = "app-amlink-submissions-mcp-prod-client"
        $ServerAppName = "app-amlink-submissions-mcp-prod-server"
        $Location = "eastus2"
    }
}

$Registry = "ghcr.io"
$Owner = "eduardomb-aw"
$ClientImage = "$Registry/$Owner/amlink-submissions-mcp-client:$ImageTag"
$ServerImage = "$Registry/$Owner/amlink-submissions-mcp-server:$ImageTag"

Write-Host "🚀 Deploying to Web Apps for Containers" -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor Cyan
Write-Host "Image Tag: $ImageTag" -ForegroundColor Cyan
Write-Host "Client Image: $ClientImage" -ForegroundColor Yellow
Write-Host "Server Image: $ServerImage" -ForegroundColor Yellow
Write-Host ""

# Verify container images exist
if (-not $SkipHealthCheck) {
    Write-Host "🔍 Verifying container images..." -ForegroundColor Blue
    
    try {
        docker manifest inspect $ClientImage | Out-Null
        Write-Host "✅ Client image verified" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Client image not found: $ClientImage" -ForegroundColor Red
        exit 1
    }
    
    try {
        docker manifest inspect $ServerImage | Out-Null
        Write-Host "✅ Server image verified" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Server image not found: $ServerImage" -ForegroundColor Red
        exit 1
    }
    Write-Host ""
}

# Check Azure login
Write-Host "🔐 Checking Azure authentication..." -ForegroundColor Blue
try {
    $account = az account show --output json | ConvertFrom-Json
    Write-Host "✅ Logged in as: $($account.user.name)" -ForegroundColor Green
    Write-Host "✅ Subscription: $($account.name)" -ForegroundColor Green
}
catch {
    Write-Host "❌ Not logged in to Azure. Please run 'az login' first." -ForegroundColor Red
    exit 1
}
Write-Host ""

# Update Client Web App
Write-Host "🌐 Updating Client Web App: $ClientAppName" -ForegroundColor Blue
try {
    az webapp config container set `
        --name $ClientAppName `
        --resource-group $ResourceGroup `
        --container-image-name $ClientImage `
        --container-registry-url "https://$Registry" `
        --output none
    
    Write-Host "✅ Client Web App updated successfully" -ForegroundColor Green
}
catch {
    Write-Host "❌ Failed to update Client Web App" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# Update Server Web App
Write-Host "🖥️ Updating Server Web App: $ServerAppName" -ForegroundColor Blue
try {
    az webapp config container set `
        --name $ServerAppName `
        --resource-group $ResourceGroup `
        --container-image-name $ServerImage `
        --container-registry-url "https://$Registry" `
        --output none
    
    Write-Host "✅ Server Web App updated successfully" -ForegroundColor Green
}
catch {
    Write-Host "❌ Failed to update Server Web App" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# Restart Web Apps
Write-Host "🔄 Restarting Web Apps..." -ForegroundColor Blue
try {
    # Restart both apps in parallel
    $clientRestart = Start-Job -ScriptBlock { 
        az webapp restart --name $using:ClientAppName --resource-group $using:ResourceGroup --output none
    }
    $serverRestart = Start-Job -ScriptBlock { 
        az webapp restart --name $using:ServerAppName --resource-group $using:ResourceGroup --output none
    }
    
    # Wait for both to complete
    Wait-Job $clientRestart, $serverRestart | Out-Null
    Remove-Job $clientRestart, $serverRestart
    
    Write-Host "✅ Both Web Apps restarted successfully" -ForegroundColor Green
}
catch {
    Write-Host "❌ Failed to restart Web Apps" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# Health check
if (-not $SkipHealthCheck) {
    Write-Host "⏳ Waiting for applications to start up..." -ForegroundColor Blue
    Start-Sleep -Seconds 60
    
    $ClientUrl = "https://$ClientAppName.azurewebsites.net"
    $ServerUrl = "https://$ServerAppName.azurewebsites.net"
    
    Write-Host "🏥 Running health checks..." -ForegroundColor Blue
    
    # Test client app
    try {
        $clientResponse = Invoke-WebRequest -Uri $ClientUrl -Method Head -TimeoutSec 30
        Write-Host "✅ Client app is healthy: $ClientUrl" -ForegroundColor Green
    }
    catch {
        Write-Host "⚠️ Client app may still be starting: $ClientUrl" -ForegroundColor Yellow
    }
    
    # Test server app
    try {
        $serverResponse = Invoke-WebRequest -Uri $ServerUrl -Method Head -TimeoutSec 30
        Write-Host "✅ Server app is healthy: $ServerUrl" -ForegroundColor Green
    }
    catch {
        Write-Host "⚠️ Server app may still be starting: $ServerUrl" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "🎉 Deployment completed successfully!" -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor Cyan
Write-Host "Client URL: https://$ClientAppName.azurewebsites.net" -ForegroundColor Cyan
Write-Host "Server URL: https://$ServerAppName.azurewebsites.net" -ForegroundColor Cyan