# Azure Web Apps Deployment Configuration Guide

This document describes the automatic environment variable configuration implemented in the deployment pipeline for Azure Web Apps.

## 🔧 Automatic Configuration Overview

The deployment pipeline (`deploy-webapps.yml`) now automatically configures all required environment variables for both client and server applications, eliminating the need for manual setup.

## 📋 Environment Variables Configured

### **Client Application Settings**

The following environment variables are automatically set for the client web app:

```bash
# Identity Server OAuth Configuration
IdentityServer__Url=https://identitydev.amwins.com
IdentityServer__ClientId=al-mcp-client
IdentityServer__ServerClientId=al-mcp-client
IdentityServer__GrantType=authorization_code
IdentityServer__Scopes=amlink-maintenance-api amlink-submission-api amlink-policy-api amlink-doc-api amwins-graphadapter-api
IdentityServer__RedirectUri=https://[CLIENT-APP-NAME].azurewebsites.net/oauth/callback
IdentityServer__ResponseMode=query

# MCP Server Communication
McpServer__Url=https://[SERVER-APP-NAME].azurewebsites.net/
McpServer__BrowserUrl=https://[SERVER-APP-NAME].azurewebsites.net/

# Development Settings
DetailedErrors=true
```

### **Server Application Settings**

The following environment variables are automatically set for the server web app:

```bash
# Identity Server OAuth Configuration
IdentityServer__Url=https://identitydev.amwins.com
IdentityServer__ClientId=al-mcp-client
IdentityServer__GrantType=authorization_code
IdentityServer__Scopes=amlink-maintenance-api amlink-submission-api amlink-policy-api amlink-doc-api amwins-graphadapter-api
IdentityServer__RedirectUri=https://[CLIENT-APP-NAME].azurewebsites.net/callback
IdentityServer__ResponseMode=query

# External API Configuration
ExternalApis__SubmissionApi__BaseUrl=https://amlink-submission-api-dev.amwins.net/v1/
ExternalApis__SubmissionApi__RequiredScope=amlink-submission-api
ExternalApis__SubmissionApi__UserAgent=mcp-submission-client
ExternalApis__SubmissionApi__Version=1.0

# MCP Server Configuration
Server__ResourceDocumentationUrl=https://docs.example.com/api/weather

# Development Settings
DetailedErrors=true
```

### **Base Infrastructure Settings**

These are set during initial provisioning and maintained across deployments:

```bash
# ASP.NET Core Configuration
ASPNETCORE_ENVIRONMENT=staging  # or production
ASPNETCORE_URLS=http://+:8080
WEBSITES_PORT=8080

# Container Registry Settings (automatic)
DOCKER_REGISTRY_SERVER_URL=https://ghcr.io
DOCKER_REGISTRY_SERVER_USERNAME=eduardomb-aw
DOCKER_REGISTRY_SERVER_PASSWORD=[GITHUB_TOKEN]

# Azure Web App Settings (automatic)
WEBSITES_ENABLE_APP_SERVICE_STORAGE=false
```

## 🔄 Deployment Process

### **When Configuration Happens**

Environment variables are configured automatically during every deployment:

1. **Container Image Update**: New container images are deployed
2. **Environment Configuration**: All app settings are updated/verified
3. **Application Restart**: Apps restart with new configuration
4. **Health Verification**: Deployment pipeline verifies app health

### **Dynamic URL Configuration**

The pipeline dynamically sets URLs based on the actual deployed app names:

```yaml
# Client configuration gets server URL
McpServer__Url=https://${{ server_app_name }}.azurewebsites.net/

# Server configuration gets client callback URL  
IdentityServer__RedirectUri=https://${{ client_app_name }}.azurewebsites.net/oauth/callback
```

## ⚙️ Manual Configuration Required

### **Sensitive Environment Variables**

The following variables must be set manually in the Azure Portal for security reasons:

**Both Applications:**
- `OPENAI_API_KEY`: Your OpenAI API key for LLM integration

**Optional (if needed):**
- `IDENTITY_SERVER_CLIENT_SECRET`: OAuth client secret

### **How to Set Manual Variables**

1. **Azure Portal Method:**
   - Go to Azure Portal
   - Navigate to your Web App (client or server)
   - Go to **Configuration** → **Application Settings**
   - Click **New application setting**
   - Add `OPENAI_API_KEY` with your API key value
   - Click **Save**

2. **Azure CLI Method:**
   ```bash
   # Set OPENAI_API_KEY for client app
   az webapp config appsettings set \
     --name app-amlink-submissions-mcp-staging-client \
     --resource-group rg-amlink-submissions-mcp-staging \
     --settings "OPENAI_API_KEY=your-openai-api-key-here"
   
   # Set OPENAI_API_KEY for server app
   az webapp config appsettings set \
     --name app-amlink-submissions-mcp-staging-server \
     --resource-group rg-amlink-submissions-mcp-staging \
     --settings "OPENAI_API_KEY=your-openai-api-key-here"
   ```

## 🔍 Verification

### **Check Current Configuration**

View current app settings:

```bash
# Check client app settings
az webapp config appsettings list \
  --name app-amlink-submissions-mcp-staging-client \
  --resource-group rg-amlink-submissions-mcp-staging \
  --output table

# Check server app settings  
az webapp config appsettings list \
  --name app-amlink-submissions-mcp-staging-server \
  --resource-group rg-amlink-submissions-mcp-staging \
  --output table
```

### **Application Health Check**

After deployment, verify applications are running:

- **Client**: https://app-amlink-submissions-mcp-staging-client.azurewebsites.net
- **Server**: https://app-amlink-submissions-mcp-staging-server.azurewebsites.net

## 🚀 Benefits of Automatic Configuration

### **Consistency**
- Same configuration applied every deployment
- No manual errors or missed settings
- Environment-specific URLs automatically generated

### **Security**
- Configuration stored in Azure, not in code
- Sensitive secrets still require manual setup
- No configuration drift between deployments

### **Maintenance**
- Updates to configuration happen in the pipeline
- Version controlled configuration changes
- Easy to replicate across environments

### **Reliability**
- Configuration applied before app restart
- Deployment pipeline verifies successful configuration
- Health checks ensure apps are working after configuration

## 🔧 Customizing Configuration

### **Updating App Settings**

To modify the automatic configuration:

1. Edit `.github/workflows/deploy-webapps.yml`
2. Update the `Configure Client App Environment Variables` or `Configure Server App Environment Variables` steps
3. Commit and push changes
4. Run deployment to apply new configuration

### **Environment-Specific Settings**

The pipeline automatically detects the environment (staging/production) and configures appropriate settings:

- **Staging**: Uses development API endpoints
- **Production**: Uses production API endpoints
- **URLs**: Dynamically generated based on actual app names

## 📋 Troubleshooting

### **Missing Environment Variables**

If apps aren't working after deployment:

1. Check deployment pipeline logs for configuration errors
2. Verify app settings in Azure Portal
3. Ensure OPENAI_API_KEY is set manually
4. Check application logs for configuration-related errors

### **OAuth/Authentication Issues**

Common issues and solutions:

- **Redirect URI mismatch**: Verify `IdentityServer__RedirectUri` matches your Identity Server configuration
- **Scope issues**: Ensure `IdentityServer__Scopes` includes all required scopes
- **Client ID mismatch**: Verify `IdentityServer__ClientId` matches your Identity Server setup

### **API Communication Issues**

If server can't access external APIs:

- Verify `ExternalApis__SubmissionApi__BaseUrl` is correct
- Check API scopes and authentication
- Ensure external APIs are accessible from Azure

---

*This configuration is automatically maintained by the deployment pipeline. Manual changes in Azure Portal may be overwritten on next deployment.*