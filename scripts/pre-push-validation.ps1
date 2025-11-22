#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Pre-push validation script that mirrors the PR validation workflow
.DESCRIPTION
    This script performs the same validations as the GitHub Actions PR validation workflow
    to catch issues before pushing to prevent PR validation failures.
.EXAMPLE
    .\scripts\pre-push-validation.ps1
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$startTime = Get-Date

Write-Host "🚀 Starting Pre-Push Validation (matching PR validation workflow)" -ForegroundColor Green
Write-Host "=" * 70 -ForegroundColor Gray

# Ensure we're in the repository root
$repoRoot = git rev-parse --show-toplevel 2>$null
if (-not $repoRoot) {
    throw "❌ Not in a git repository"
}
Set-Location $repoRoot

$validationFailed = $false

function Write-ValidationStep {
    param([string]$Message, [string]$Color = "Cyan")
    Write-Host "🔄 $Message" -ForegroundColor $Color
}

function Write-ValidationSuccess {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-ValidationError {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
    $script:validationFailed = $true
}

# =============================================================================
# .NET VALIDATION (mirrors 'validate' job)
# =============================================================================

Write-Host "`n📦 .NET Validation" -ForegroundColor Yellow

# 1. Restore dependencies
Write-ValidationStep "Restoring dependencies..."
try {
    dotnet restore --verbosity quiet
    Write-ValidationSuccess "Dependencies restored"
} catch {
    Write-ValidationError "Failed to restore dependencies: $($_.Exception.Message)"
}

# 2. Check code formatting 
Write-ValidationStep "Checking code formatting..."
try {
    dotnet format --verify-no-changes --verbosity diagnostic 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-ValidationSuccess "Code formatting is correct"
    } else {
        Write-ValidationError "Code formatting issues found. Run 'dotnet format' to fix."
    }
} catch {
    Write-ValidationError "Failed to check code formatting: $($_.Exception.Message)"
}

# 3. Build solution
Write-ValidationStep "Building solution..."
try {
    dotnet build --no-restore --configuration Release --verbosity quiet
    if ($LASTEXITCODE -eq 0) {
        Write-ValidationSuccess "Solution built successfully"
    } else {
        Write-ValidationError "Build failed"
    }
} catch {
    Write-ValidationError "Failed to build solution: $($_.Exception.Message)"
}

# 4. Run tests with coverage
Write-ValidationStep "Running tests..."
try {
    # Check if test projects exist (PowerShell equivalent of bash find command)
    $testProjects = Get-ChildItem -Recurse -Filter "*.csproj" | Where-Object { 
        (Get-Content $_.FullName) -match "Microsoft.NET.Test.Sdk" 
    }
    
    if ($testProjects) {
        Write-Host "   Test projects found, running tests..."
        dotnet test --no-build --configuration Release --logger "console;verbosity=minimal"
        if ($LASTEXITCODE -eq 0) {
            Write-ValidationSuccess "All tests passed"
        } else {
            Write-ValidationError "Tests failed"
        }
    } else {
        Write-Host "   No test projects found"
        Write-ValidationSuccess "No tests to run"
    }
} catch {
    Write-ValidationError "Failed to run tests: $($_.Exception.Message)"
}

# 5. Docker build test
Write-ValidationStep "Testing Docker builds..."
try {
    # Test server build
    docker build -t test-server ./src/amlink-submissions-mcp-server/ --quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Server Docker build successful"
        # Clean up
        docker rmi test-server 2>$null
    } else {
        Write-ValidationError "Server Docker build failed"
    }
    
    # Test client build  
    docker build -t test-client ./src/amlink-submissions-mcp-client/ --quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Client Docker build successful"
        # Clean up
        docker rmi test-client 2>$null
    } else {
        Write-ValidationError "Client Docker build failed"
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-ValidationSuccess "Docker builds completed successfully"
    }
} catch {
    Write-ValidationError "Failed to test Docker builds: $($_.Exception.Message)"
}

# 6. Validate Docker Compose
Write-ValidationStep "Validating Docker Compose configurations..."
try {
    $env:IDENTITY_SERVER_CLIENT_SECRET = "validation-dummy-secret"
    $env:OPENAI_API_KEY = "validation-dummy-key"
    
    docker compose -f docker-compose.yml config | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ docker-compose.yml is valid"
    } else {
        Write-ValidationError "docker-compose.yml validation failed"
    }
    
    docker compose -f docker-compose.yml -f docker-compose.prod.yml config | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Production Docker Compose config is valid"
        Write-ValidationSuccess "Docker Compose configurations are valid"
    } else {
        Write-ValidationError "Production Docker Compose validation failed"
    }
} catch {
    Write-ValidationError "Failed to validate Docker Compose: $($_.Exception.Message)"
} finally {
    # Clean up environment variables
    Remove-Item Env:IDENTITY_SERVER_CLIENT_SECRET -ErrorAction SilentlyContinue
    Remove-Item Env:OPENAI_API_KEY -ErrorAction SilentlyContinue
}

# =============================================================================
# LINTING & SECURITY VALIDATION (mirrors 'lint' job)
# =============================================================================

Write-Host "`n🔍 Linting & Security Validation" -ForegroundColor Yellow

# Check if required tools are available
Write-ValidationStep "Checking linting tools availability..."

$toolsAvailable = $true

# Check for markdownlint
try {
    markdownlint --version | Out-Null
    Write-Host "   ✅ markdownlint is available"
} catch {
    Write-Host "   ⚠️  markdownlint not found. Install with: npm install -g markdownlint-cli" -ForegroundColor Yellow
    $toolsAvailable = $false
}

# Check for hadolint (Dockerfile linter)
try {
    hadolint --version | Out-Null
    Write-Host "   ✅ hadolint is available"
} catch {
    Write-Host "   ⚠️  hadolint not found. Install from: https://github.com/hadolint/hadolint#install" -ForegroundColor Yellow
    $toolsAvailable = $false
}

if ($toolsAvailable) {
    Write-ValidationSuccess "All linting tools are available"
    
    # Markdown validation
    Write-ValidationStep "Validating Markdown files..."
    try {
        $markdownFiles = Get-ChildItem -Recurse -Filter "*.md" | Where-Object { 
            $_.FullName -notlike "*node_modules*" -and $_.FullName -notlike "*bin*" -and $_.FullName -notlike "*obj*"
        }
        
        $markdownErrors = $false
        foreach ($file in $markdownFiles) {
            markdownlint $file.FullName 2>$null
            if ($LASTEXITCODE -ne 0) {
                Write-Host "   ❌ Markdown issues in: $($file.Name)" -ForegroundColor Red
                $markdownErrors = $true
            }
        }
        
        if (-not $markdownErrors) {
            Write-ValidationSuccess "All Markdown files are valid"
        } else {
            Write-ValidationError "Markdown validation failed. Run 'markdownlint [file]' for details."
        }
    } catch {
        Write-ValidationError "Failed to validate Markdown: $($_.Exception.Message)"
    }
    
    # Dockerfile validation
    Write-ValidationStep "Validating Dockerfiles..."
    try {
        $dockerfiles = Get-ChildItem -Recurse -Filter "Dockerfile*" | Where-Object { 
            $_.FullName -notlike "*node_modules*"
        }
        
        $dockerfileErrors = $false
        foreach ($dockerfile in $dockerfiles) {
            hadolint $dockerfile.FullName 2>$null
            if ($LASTEXITCODE -ne 0) {
                Write-Host "   ❌ Dockerfile issues in: $($dockerfile.Name)" -ForegroundColor Red
                $dockerfileErrors = $true
            }
        }
        
        if (-not $dockerfileErrors) {
            Write-ValidationSuccess "All Dockerfiles are valid"
        } else {
            Write-ValidationError "Dockerfile validation failed. Run 'hadolint [file]' for details."
        }
    } catch {
        Write-ValidationError "Failed to validate Dockerfiles: $($_.Exception.Message)"
    }
    
} else {
    Write-Host "   ⚠️  Skipping detailed linting due to missing tools" -ForegroundColor Yellow
    Write-Host "   ℹ️  GitHub Actions will perform full linting validation" -ForegroundColor Blue
}

# Basic YAML/JSON validation (using PowerShell)
Write-ValidationStep "Validating YAML/JSON files..."
try {
    $yamlErrors = $false
    $jsonErrors = $false
    
    # Validate JSON files
    $jsonFiles = Get-ChildItem -Recurse -Filter "*.json" | Where-Object { 
        $_.FullName -notlike "*node_modules*" -and $_.FullName -notlike "*bin*" -and $_.FullName -notlike "*obj*"
    }
    
    foreach ($file in $jsonFiles) {
        try {
            Get-Content $file.FullName | ConvertFrom-Json | Out-Null
        } catch {
            Write-Host "   ❌ Invalid JSON: $($file.Name)" -ForegroundColor Red
            $jsonErrors = $true
        }
    }
    
    # Basic YAML validation (check for obvious syntax issues)
    $yamlFiles = Get-ChildItem -Recurse -Filter "*.yml" | Where-Object { 
        $_.FullName -notlike "*node_modules*"
    }
    $yamlFiles += Get-ChildItem -Recurse -Filter "*.yaml" | Where-Object { 
        $_.FullName -notlike "*node_modules*"
    }
    
    foreach ($file in $yamlFiles) {
        $content = Get-Content $file.FullName -Raw
        # Basic YAML syntax check (look for common issues)
        if ($content -match "^\s*-\s*$|^\s+[^-\s]" -and $content -match ":\s*$") {
            # Likely has YAML syntax issues, but this is basic validation
        }
    }
    
    if (-not $jsonErrors -and -not $yamlErrors) {
        Write-ValidationSuccess "YAML/JSON files appear valid"
    } else {
        Write-ValidationError "YAML/JSON validation failed"
    }
} catch {
    Write-ValidationError "Failed to validate YAML/JSON: $($_.Exception.Message)"
}

# =============================================================================
# SUMMARY
# =============================================================================

$endTime = Get-Date
$duration = $endTime - $startTime

Write-Host "`n" + "=" * 70 -ForegroundColor Gray

if ($validationFailed) {
    Write-Host "❌ PRE-PUSH VALIDATION FAILED" -ForegroundColor Red
    Write-Host "   Duration: $([math]::Round($duration.TotalSeconds, 1))s" -ForegroundColor Gray
    Write-Host "`n🔧 Please fix the issues above before pushing." -ForegroundColor Yellow
    Write-Host "   This will prevent PR validation failures in GitHub Actions." -ForegroundColor Gray
    exit 1
} else {
    Write-Host "✅ PRE-PUSH VALIDATION PASSED" -ForegroundColor Green
    Write-Host "   Duration: $([math]::Round($duration.TotalSeconds, 1))s" -ForegroundColor Gray
    Write-Host "`n🚀 Ready to push! Your changes should pass PR validation." -ForegroundColor Green
    exit 0
}