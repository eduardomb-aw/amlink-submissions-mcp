# Azure Container Apps Setup Script
# This script helps configure Azure resources and GitHub secrets for deployment

param(
    [Parameter(Mandatory=$false)]
    [string]$SubscriptionId = "1f47ba90-a3ed-4e70-a902-570616cb62b0",
    
    [Parameter(Mandatory=$false)]
    [string]$ResourceLocation = "East US 2",
    
    [Parameter(Mandatory=$false)]
    [string]$ServicePrincipalName = "amlink-submissions-mcp-github-actions"
)

# Colors for output
$Red = [System.ConsoleColor]::Red
$Green = [System.ConsoleColor]::Green
$Yellow = [System.ConsoleColor]::Yellow
$Blue = [System.ConsoleColor]::Blue

function Write-ColorText {
    param([string]$Text, [System.ConsoleColor]$Color)
    Write-Host $Text -ForegroundColor $Color
}

function Test-AzureLogin {
    try {
        $context = Get-AzContext
        if ($null -eq $context) {
            return $false
        }
        return $true
    } catch {
        return $false
    }
}

Write-ColorText "🚀 Azure Container Apps Setup for AmLink MCP" $Blue
Write-ColorText "================================================" $Blue
Write-ColorText ""

# Check if Azure PowerShell is installed
Write-ColorText "📋 Checking prerequisites..." $Blue
try {
    Import-Module Az -ErrorAction Stop
    Write-ColorText "✅ Azure PowerShell module is installed" $Green
} catch {
    Write-ColorText "❌ Azure PowerShell module not found" $Red
    Write-ColorText "Please install: Install-Module -Name Az -AllowClobber -Scope CurrentUser" $Yellow
    exit 1
}

# Check Azure login
if (-not (Test-AzureLogin)) {
    Write-ColorText "🔐 Please login to Azure..." $Yellow
    Connect-AzAccount
}

# Set subscription context
Write-ColorText "🎯 Setting subscription context..." $Blue
try {
    Set-AzContext -SubscriptionId $SubscriptionId
    $subscription = Get-AzSubscription -SubscriptionId $SubscriptionId
    Write-ColorText "✅ Using subscription: $($subscription.Name)" $Green
} catch {
    Write-ColorText "❌ Failed to set subscription context" $Red
    Write-ColorText "Please verify subscription ID: $SubscriptionId" $Yellow
    exit 1
}

# Create service principal
Write-ColorText "👤 Creating service principal for GitHub Actions..." $Blue
try {
    # Check if service principal already exists
    $existingSp = Get-AzADServicePrincipal -DisplayName $ServicePrincipalName -ErrorAction SilentlyContinue
    
    if ($existingSp) {
        Write-ColorText "⚠️  Service principal already exists: $ServicePrincipalName" $Yellow
        $servicePrincipal = $existingSp
        
        # Create new credential
        $credential = New-AzADSpCredential -ObjectId $servicePrincipal.Id
        $clientSecret = $credential.SecretText
    } else {
        # Create new service principal
        $servicePrincipal = New-AzADServicePrincipal -DisplayName $ServicePrincipalName -Role "Contributor"
        $clientSecret = $servicePrincipal.PasswordCredentials.SecretText
    }
    
    Write-ColorText "✅ Service principal created/updated successfully" $Green
    Write-ColorText "   Client ID: $($servicePrincipal.AppId)" $Green
} catch {
    Write-ColorText "❌ Failed to create service principal" $Red
    Write-ColorText "Error: $($_.Exception.Message)" $Yellow
    exit 1
}

# Get tenant information
$tenant = Get-AzTenant
$tenantId = $tenant.Id

Write-ColorText ""
Write-ColorText "🔑 GitHub Repository Secrets Configuration" $Blue
Write-ColorText "============================================" $Blue
Write-ColorText ""
Write-ColorText "Add these secrets to your GitHub repository:" $Yellow
Write-ColorText "(Settings → Secrets and variables → Actions → New repository secret)" $Yellow
Write-ColorText ""

Write-ColorText "Azure Authentication Secrets:" $Blue
Write-ColorText "AZURE_CLIENT_ID=$($servicePrincipal.AppId)" $Green
Write-ColorText "AZURE_TENANT_ID=$tenantId" $Green
Write-ColorText "AZURE_SUBSCRIPTION_ID=$SubscriptionId" $Green
Write-ColorText ""

Write-ColorText "Application Configuration Secrets:" $Blue
Write-ColorText "CLIENT_SECRET=<your-secure-client-secret-here>" $Yellow
Write-ColorText "SUBMISSION_API_KEY=<your-submission-api-key-here>" $Yellow
Write-ColorText "CERT_PASSWORD=<your-certificate-password-here>" $Yellow
Write-ColorText ""

# Verify permissions
Write-ColorText "🔍 Verifying service principal permissions..." $Blue
try {
    $roleAssignments = Get-AzRoleAssignment -ObjectId $servicePrincipal.Id
    $contributorRole = $roleAssignments | Where-Object { $_.RoleDefinitionName -eq "Contributor" }
    
    if ($contributorRole) {
        Write-ColorText "✅ Service principal has Contributor role" $Green
    } else {
        Write-ColorText "⚠️  Service principal missing Contributor role" $Yellow
        Write-ColorText "   Adding Contributor role..." $Blue
        New-AzRoleAssignment -ObjectId $servicePrincipal.Id -RoleDefinitionName "Contributor"
        Write-ColorText "✅ Contributor role assigned" $Green
    }
} catch {
    Write-ColorText "⚠️  Could not verify permissions: $($_.Exception.Message)" $Yellow
}

# Create resource groups
Write-ColorText "📦 Creating resource groups..." $Blue
$environments = @("staging", "prod")

foreach ($env in $environments) {
    $rgName = "rg-amlink-submissions-mcp-$env"
    try {
        $rg = Get-AzResourceGroup -Name $rgName -ErrorAction SilentlyContinue
        if ($rg) {
            Write-ColorText "✅ Resource group already exists: $rgName" $Green
        } else {
            New-AzResourceGroup -Name $rgName -Location $ResourceLocation -Tag @{
                environment = $env
                project = "amlink-submissions-mcp"
                managedBy = "github-actions"
            }
            Write-ColorText "✅ Created resource group: $rgName" $Green
        }
    } catch {
        Write-ColorText "❌ Failed to create resource group: $rgName" $Red
        Write-ColorText "Error: $($_.Exception.Message)" $Yellow
    }
}

Write-ColorText ""
Write-ColorText "🎉 Setup Complete!" $Green
Write-ColorText "==================" $Green
Write-ColorText ""
Write-ColorText "Next steps:" $Blue
Write-ColorText "1. Add the GitHub secrets shown above to your repository" $Yellow
Write-ColorText "2. Configure your application secrets (CLIENT_SECRET, etc.)" $Yellow
Write-ColorText "3. Test deployment with: 'Deploy to Azure Container Apps' workflow" $Yellow
Write-ColorText "4. Access your applications at the URLs provided after deployment" $Yellow
Write-ColorText ""
Write-ColorText "📚 For detailed instructions, see: docs/azure-deployment.md" $Blue

# Save configuration to file
$configFile = "azure-setup-config.json"
$config = @{
    subscriptionId = $SubscriptionId
    tenantId = $tenantId
    clientId = $servicePrincipal.AppId
    resourceLocation = $ResourceLocation
    resourceGroups = @{
        staging = "rg-amlink-submissions-mcp-staging"
        production = "rg-amlink-submissions-mcp-prod"
    }
    servicePrincipalName = $ServicePrincipalName
    setupDate = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
}

$config | ConvertTo-Json -Depth 3 | Out-File -FilePath $configFile -Encoding UTF8
Write-ColorText "💾 Configuration saved to: $configFile" $Blue