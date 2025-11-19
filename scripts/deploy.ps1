# Deploy from Container Registry Script (PowerShell)
# This script helps deploy the application using published container images

param(
    [Parameter(Position=0)]
    [ValidateSet("deploy", "stop", "logs", "status", "cleanup")]
    [string]$Command,
    
    [string]$Registry = "ghcr.io",
    [string]$Tag = "latest",
    [string]$EnvFile = ".env.prod",
    [string]$ComposeFile = "docker-compose.registry.yml",
    [switch]$Help
)

function Show-Usage {
    Write-Host ""
    Write-Host "Usage: .\deploy.ps1 [OPTIONS] COMMAND" -ForegroundColor Blue
    Write-Host ""
    Write-Host "Commands:" -ForegroundColor Green
    Write-Host "  deploy    Deploy the application using published images"
    Write-Host "  stop      Stop the running application"
    Write-Host "  logs      Show application logs"
    Write-Host "  status    Show application status"
    Write-Host "  cleanup   Remove containers and networks"
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Green
    Write-Host "  -Registry REGISTRY    Container registry (default: ghcr.io)"
    Write-Host "  -Tag TAG             Image tag (default: latest)"
    Write-Host "  -EnvFile FILE        Environment file (default: .env.prod)"
    Write-Host "  -Help                Show this help message"
    Write-Host ""
}

function Test-Prerequisites {
    Write-Host "Checking prerequisites..." -ForegroundColor Blue
    
    # Check if docker is installed
    try {
        docker --version | Out-Null
    } catch {
        Write-Host "Error: Docker is not installed" -ForegroundColor Red
        exit 1
    }
    
    # Check if docker-compose is available
    $composeAvailable = $false
    try {
        docker compose version | Out-Null
        $composeAvailable = $true
    } catch {
        try {
            docker-compose --version | Out-Null
            $composeAvailable = $true
        } catch {
            Write-Host "Error: Docker Compose is not available" -ForegroundColor Red
            exit 1
        }
    }
    
    # Check if environment file exists
    if (!(Test-Path $EnvFile)) {
        Write-Host "Warning: Environment file $EnvFile not found" -ForegroundColor Yellow
        Write-Host "Creating from template..." -ForegroundColor Yellow
        if (Test-Path ".env.prod.example") {
            Copy-Item ".env.prod.example" $EnvFile
            Write-Host "Please edit $EnvFile with your actual values before deploying" -ForegroundColor Yellow
        } else {
            Write-Host "Error: No environment template found" -ForegroundColor Red
            exit 1
        }
    }
    
    Write-Host "Prerequisites check passed" -ForegroundColor Green
}

function Start-Deployment {
    Write-Host "Deploying amlink-submissions-mcp..." -ForegroundColor Blue
    Write-Host "Registry: $Registry" -ForegroundColor Blue
    Write-Host "Tag: $Tag" -ForegroundColor Blue
    Write-Host "Environment: $EnvFile" -ForegroundColor Blue
    
    Test-Prerequisites
    
    # Set environment variables
    $env:REGISTRY = $Registry
    $env:CLIENT_IMAGE_TAG = $Tag
    $env:SERVER_IMAGE_TAG = $Tag
    
    # Pull latest images
    Write-Host "Pulling container images..." -ForegroundColor Blue
    docker compose -f $ComposeFile --env-file $EnvFile pull
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to pull images" -ForegroundColor Red
        exit 1
    }
    
    # Start services
    Write-Host "Starting services..." -ForegroundColor Blue
    docker compose -f $ComposeFile --env-file $EnvFile up -d
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to start services" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Deployment completed!" -ForegroundColor Green
    Write-Host "Client: https://localhost:8443" -ForegroundColor Green
    Write-Host "Server: https://localhost:9443" -ForegroundColor Green
}

function Stop-Application {
    Write-Host "Stopping amlink-submissions-mcp..." -ForegroundColor Blue
    docker compose -f $ComposeFile --env-file $EnvFile stop
    Write-Host "Application stopped" -ForegroundColor Green
}

function Show-Logs {
    Write-Host "Showing application logs..." -ForegroundColor Blue
    docker compose -f $ComposeFile --env-file $EnvFile logs -f
}

function Show-Status {
    Write-Host "Application status:" -ForegroundColor Blue
    docker compose -f $ComposeFile --env-file $EnvFile ps
}

function Remove-Application {
    Write-Host "Cleaning up containers and networks..." -ForegroundColor Blue
    docker compose -f $ComposeFile --env-file $EnvFile down --remove-orphans
    Write-Host "Cleanup completed" -ForegroundColor Green
}

# Show help if requested or no command provided
if ($Help -or !$Command) {
    Show-Usage
    exit 0
}

# Execute command
switch ($Command) {
    "deploy" { Start-Deployment }
    "stop" { Stop-Application }
    "logs" { Show-Logs }
    "status" { Show-Status }
    "cleanup" { Remove-Application }
    default {
        Write-Host "Unknown command: $Command" -ForegroundColor Red
        Show-Usage
        exit 1
    }
}