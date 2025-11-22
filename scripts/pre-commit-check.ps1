#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Pre-commit validation script for AmLink Submissions MCP
.DESCRIPTION
    Runs mandatory formatting checks, build validation, and tests before allowing commits.
    This script ensures code quality and prevents PR validation failures.
.EXAMPLE
    .\scripts\pre-commit-check.ps1
.NOTES
    Run this script before every commit to avoid PR validation failures.
#>

param(
    [switch]$SkipTests,
    [switch]$FixFormatting
)

Write-Host "🚀 AmLink Submissions MCP - Pre-Commit Validation" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

$ErrorActionPreference = "Stop"
$success = $true

try {
    # Step 1: Check code formatting
    Write-Host "`n📝 Checking code formatting..." -ForegroundColor Yellow
    $formatResult = dotnet format --verify-no-changes 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Code formatting issues detected!" -ForegroundColor Red
        Write-Host $formatResult -ForegroundColor Red
        
        if ($FixFormatting) {
            Write-Host "`n🔧 Fixing formatting issues automatically..." -ForegroundColor Yellow
            dotnet format
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✅ Code formatting fixed!" -ForegroundColor Green
            } else {
                Write-Host "❌ Failed to fix formatting issues!" -ForegroundColor Red
                $success = $false
            }
        } else {
            Write-Host "`n💡 Run with -FixFormatting to automatically fix issues, or run:" -ForegroundColor Blue
            Write-Host "   dotnet format" -ForegroundColor Gray
            $success = $false
        }
    } else {
        Write-Host "✅ Code formatting is clean!" -ForegroundColor Green
    }

    # Step 2: Check markdown linting
    Write-Host "`n📄 Checking markdown formatting..." -ForegroundColor Yellow
    
    # Check if markdownlint-cli2 is available
    $markdownFiles = Get-ChildItem -Path . -Filter "*.md" -Recurse | Where-Object { $_.FullName -notmatch "node_modules|\\bin\\|\\obj\\" }
    
    if ($markdownFiles.Count -gt 0) {
        try {
            # Use npx to run markdownlint-cli2 temporarily
            $env:NODE_OPTIONS = "--no-warnings"
            $markdownResult = npx --yes markdownlint-cli2@latest "**/*.md" "!**/node_modules/**" "!**/bin/**" "!**/obj/**" 2>&1
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✅ Markdown formatting is clean!" -ForegroundColor Green
            } else {
                Write-Host "❌ Markdown linting issues detected!" -ForegroundColor Red
                Write-Host $markdownResult -ForegroundColor Red
                Write-Host "`n💡 Fix markdown issues manually or check the super-linter configuration" -ForegroundColor Blue
                $success = $false
            }
        } catch {
            Write-Host "⚠️  Markdown linting skipped (markdownlint-cli2 not available)" -ForegroundColor Yellow
            Write-Host "💡 Install Node.js and run: npm install -g markdownlint-cli2" -ForegroundColor Blue
        }
    } else {
        Write-Host "ℹ️  No markdown files found to lint" -ForegroundColor Blue
    }

    # Step 3: Build solution
    Write-Host "`n🔨 Building solution..." -ForegroundColor Yellow
    dotnet build --configuration Release --verbosity quiet
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Build successful!" -ForegroundColor Green
    } else {
        Write-Host "❌ Build failed!" -ForegroundColor Red
        $success = $false
    }

    # Step 4: Run tests (unless skipped)
    if (-not $SkipTests) {
        Write-Host "`n🧪 Running tests..." -ForegroundColor Yellow
        dotnet test --configuration Release --verbosity quiet --no-build
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ All tests passed!" -ForegroundColor Green
        } else {
            Write-Host "❌ Tests failed!" -ForegroundColor Red
            $success = $false
        }
    } else {
        Write-Host "`n⚠️  Tests skipped!" -ForegroundColor Yellow
    }

    # Summary
    Write-Host "`n=================================================" -ForegroundColor Cyan
    if ($success) {
        Write-Host "🎉 Pre-commit validation PASSED!" -ForegroundColor Green
        Write-Host "✅ Your code is ready to commit and push!" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "💥 Pre-commit validation FAILED!" -ForegroundColor Red
        Write-Host "❌ Please fix the issues before committing!" -ForegroundColor Red
        exit 1
    }

} catch {
    Write-Host "`n💥 Unexpected error during validation:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}