#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Setup script for installing pre-push validation tools
.DESCRIPTION
    Installs the required tools for local validation that mirrors GitHub Actions PR validation
.EXAMPLE
    .\scripts\setup-validation-tools.ps1
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Continue"  # Continue on errors to try installing other tools

Write-Host "🔧 Setting up Pre-Push Validation Tools" -ForegroundColor Green
Write-Host "=" * 50 -ForegroundColor Gray

function Test-CommandExists {
    param([string]$Command)
    try {
        Get-Command $Command -ErrorAction Stop | Out-Null
        return $true
    } catch {
        return $false
    }
}

# Check Node.js (required for markdownlint)
Write-Host "`n📦 Checking Node.js..."
if (Test-CommandExists "node") {
    $nodeVersion = node --version
    Write-Host "✅ Node.js is installed: $nodeVersion" -ForegroundColor Green
    
    # Install markdownlint-cli
    Write-Host "`n📝 Installing markdownlint-cli..."
    if (Test-CommandExists "markdownlint") {
        Write-Host "✅ markdownlint is already installed" -ForegroundColor Green
    } else {
        Write-Host "🔄 Installing markdownlint-cli globally..."
        npm install -g markdownlint-cli
        if (Test-CommandExists "markdownlint") {
            Write-Host "✅ markdownlint-cli installed successfully" -ForegroundColor Green
        } else {
            Write-Host "❌ Failed to install markdownlint-cli" -ForegroundColor Red
        }
    }
} else {
    Write-Host "❌ Node.js not found. Please install Node.js from https://nodejs.org/" -ForegroundColor Red
    Write-Host "   Required for markdownlint-cli installation" -ForegroundColor Yellow
}

# Install hadolint (Dockerfile linter)
Write-Host "`n🐳 Installing hadolint (Dockerfile linter)..."
if (Test-CommandExists "hadolint") {
    Write-Host "✅ hadolint is already installed" -ForegroundColor Green
} else {
    Write-Host "🔄 Installing hadolint..."
    
    if ($IsWindows -or $env:OS -eq "Windows_NT") {
        # Windows installation
        Write-Host "   Detected Windows - Installing via PowerShell..."
        try {
            # Download latest hadolint binary for Windows
            $url = "https://github.com/hadolint/hadolint/releases/latest/download/hadolint-Windows-x86_64.exe"
            $destination = "$env:USERPROFILE\hadolint.exe"
            
            Write-Host "   Downloading hadolint..."
            Invoke-WebRequest -Uri $url -OutFile $destination
            
            # Add to PATH if not already there
            $userPath = [Environment]::GetEnvironmentVariable("PATH", "User")
            $hadolintDir = Split-Path $destination
            
            if ($userPath -notlike "*$hadolintDir*") {
                Write-Host "   Adding hadolint to PATH..."
                [Environment]::SetEnvironmentVariable("PATH", "$userPath;$hadolintDir", "User")
                $env:PATH = "$env:PATH;$hadolintDir"
            }
            
            if (Test-CommandExists "hadolint") {
                Write-Host "✅ hadolint installed successfully" -ForegroundColor Green
            } else {
                Write-Host "⚠️  hadolint installed but not in PATH. You may need to restart your terminal." -ForegroundColor Yellow
            }
        } catch {
            Write-Host "❌ Failed to install hadolint: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "   Please install manually from: https://github.com/hadolint/hadolint#install" -ForegroundColor Yellow
        }
    } else {
        # macOS/Linux installation
        Write-Host "   Please install hadolint manually:" -ForegroundColor Yellow
        Write-Host "   - macOS: brew install hadolint" -ForegroundColor Gray
        Write-Host "   - Linux: Visit https://github.com/hadolint/hadolint#install" -ForegroundColor Gray
    }
}

# Check Docker
Write-Host "`n🐳 Checking Docker..."
if (Test-CommandExists "docker") {
    Write-Host "✅ Docker is installed" -ForegroundColor Green
    
    # Test docker compose (new syntax)
    docker compose version 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Docker Compose (v2) is available" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Docker Compose v2 not available, checking legacy..." -ForegroundColor Yellow
        docker-compose version 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Docker Compose (legacy) is available" -ForegroundColor Green
        } else {
            Write-Host "❌ Docker Compose not found" -ForegroundColor Red
        }
    }
} else {
    Write-Host "❌ Docker not found. Please install Docker Desktop" -ForegroundColor Red
    Write-Host "   Required for Docker build and compose validation" -ForegroundColor Yellow
}

# Check .NET SDK
Write-Host "`n🔹 Checking .NET SDK..."
if (Test-CommandExists "dotnet") {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET SDK is installed: $dotnetVersion" -ForegroundColor Green
} else {
    Write-Host "❌ .NET SDK not found. Please install from https://dotnet.microsoft.com/download" -ForegroundColor Red
}

# Summary
Write-Host "`n" + "=" * 50 -ForegroundColor Gray
Write-Host "🎉 Setup Summary:" -ForegroundColor Green
Write-Host ""

$tools = @(
    @{ Name = "Node.js"; Command = "node" },
    @{ Name = "markdownlint"; Command = "markdownlint" },
    @{ Name = "hadolint"; Command = "hadolint" },
    @{ Name = "Docker"; Command = "docker" },
    @{ Name = ".NET SDK"; Command = "dotnet" }
)

foreach ($tool in $tools) {
    if (Test-CommandExists $tool.Command) {
        Write-Host "✅ $($tool.Name)" -ForegroundColor Green
    } else {
        Write-Host "❌ $($tool.Name)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "📋 Next Steps:" -ForegroundColor Cyan
Write-Host "   1. Restart your terminal/PowerShell to refresh PATH" -ForegroundColor Gray
Write-Host "   2. Run: .\scripts\pre-push-validation.ps1" -ForegroundColor Gray
Write-Host "   3. Fix any validation issues before pushing" -ForegroundColor Gray
Write-Host ""
Write-Host "💡 For missing tools, check the installation links above." -ForegroundColor Blue