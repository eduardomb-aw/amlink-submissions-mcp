# Azure Web Apps Deployment Guide

This guide walks through the automated deployment to Azure Web Apps for Containers for the AmLink Submissions MCP project.

## 🎯 Current Deployment Architecture

**Production Environment:**
- **Subscription**: Architecture Playground (`1f47ba90-a3ed-4e70-a902-570616cb62b0`)
- **Location**: East US 2
- **Resource Group**: `rg-amlink-submissions-mcp-staging`
- **Client Web App**: `app-amlink-submissions-mcp-staging-client`
- **Server Web App**: `app-amlink-submissions-mcp-staging-server`
- **VNet Integration**: ArchPlayGroundAFRG-1 (Subnet 5)
- **Deployment Method**: Automated via GitHub Actions
- **Configuration**: Automatic environment variable setup

## 🎯 Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Azure Container Apps                    │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────────┐    ┌─────────────────────────────┐  │
│  │    MCP Client       │    │       MCP Server            │  │
│  │    (Web UI)         │    │    (API + Identity)         │  │
│  │                     │    │                             │  │
│  │  • Auto-scaling     │    │  • Auto-scaling             │  │
│  │  • Health checks    │    │  • Health checks            │  │
│  │  • HTTPS endpoint   │    │  • HTTPS endpoint           │  │
│  └─────────────────────┘    └─────────────────────────────┘  │
├─────────────────────────────────────────────────────────────┤
│                Container Apps Environment                   │
├─────────────────────────────────────────────────────────────┤
│              Log Analytics Workspace                       │
└─────────────────────────────────────────────────────────────┘
```

## 🚀 Quick Setup

### 1. Azure Prerequisites

**VNet Integration:**
- Uses existing VNet: `ArchPlayGroundAFRG-1` in resource group `NewAFormentiRG`
- Provides secure network access for downstream API calls
- Subnet: `5` (10.202.58.128/25)

**Create Azure Service Principal:**
```bash
# Login to Azure
az login

# Create service principal for deployment
az ad sp create-for-rbac \
  --name "amlink-mcp-github-actions" \
  --role "Contributor" \
  --scopes "/subscriptions/YOUR_SUBSCRIPTION_ID" \
  --sdk-auth
```

**Note the output** - you'll need these values for GitHub secrets.

### 2. GitHub Repository Secrets

Add these secrets to your GitHub repository (Settings → Secrets and variables → Actions):

**Azure Authentication:**
- `AZURE_CLIENT_ID` - From service principal output
- `AZURE_TENANT_ID` - From service principal output  
- `AZURE_SUBSCRIPTION_ID` - Your Azure subscription ID

**Application Configuration:**
- `CLIENT_SECRET` - Secure client secret for Identity Server
- `SUBMISSION_API_KEY` - API key for external submission service
- `CERT_PASSWORD` - Certificate password (optional, for custom certs)

### 3. Deploy to Azure

**Step 1: Provision Infrastructure**
1. Go to GitHub Actions
2. Select "Provision Web Apps Infrastructure" 
3. Click "Run workflow"
4. Choose environment (staging/production) and location
5. This automatically creates:
   - Resource Group and App Service Plan
   - Client and Server Web Apps
   - VNet integration with ArchPlayGroundAFRG-1
   - Basic container registry authentication

**Step 2: Deploy Applications**
1. Select "Deploy to Web Apps for Containers"
2. Choose environment and image tag
3. This configures all environment variables and deploys containers

**Option 2: Automatic on Release**
- Creates staging deployment for pre-releases
- Creates production deployment for stable releases

## 📋 Deployment Environments

### **Staging Environment**
- **Resource Group:** `rg-amlink-submissions-mcp-staging`
- **Client Web App:** `app-amlink-submissions-mcp-staging-client`
- **Server Web App:** `app-amlink-submissions-mcp-staging-server`
- **Purpose:** Testing and validation
- **Auto-deploy:** Manual trigger or releases
- **Client URL:** `https://app-amlink-submissions-mcp-staging-client.azurewebsites.net`
- **Server URL:** `https://app-amlink-submissions-mcp-staging-server.azurewebsites.net`

### **Production Environment**  
- **Resource Group:** `rg-amlink-submissions-mcp-prod`
- **Client Web App:** `app-amlink-submissions-mcp-prod-client`
- **Server Web App:** `app-amlink-submissions-mcp-prod-server`
- **Purpose:** Live production workloads
- **Auto-deploy:** Stable releases (v1.0.0)
- **Client URL:** `https://app-amlink-submissions-mcp-prod-client.azurewebsites.net`
- **Server URL:** `https://app-amlink-submissions-mcp-prod-server.azurewebsites.net`

## 🔧 Infrastructure Components

### **App Service Plan**
- **SKU:** B1 (Basic) Linux containers
- **Location:** East US 2
- **Shared:** Both client and server Web Apps

### **VNet Integration**
- **VNet:** ArchPlayGroundAFRG-1 (NewAFormentiRG resource group)
- **Subnet:** Subnet "5" (10.202.58.128/25)
- **Delegation:** Microsoft.Web/serverFarms (automatic)
- **Same Subscription:** Web Apps and VNet in Architecture Playground
- **Benefits:** Internal resource access, network security

### **MCP Server Web App**
- **Image:** `ghcr.io/eduardomb-aw/amlink-submissions-mcp-server`
- **Port:** 8080 (HTTP), auto-HTTPS via Azure
- **VNet:** Integrated with internal network access
- **Configuration:** Automatic environment variable setup

### **MCP Client Web App**
- **Image:** `ghcr.io/eduardomb-aw/amlink-submissions-mcp-client`
- **Port:** 8080 (HTTP), auto-HTTPS via Azure
- **VNet:** Integrated with internal network access
- **Configuration:** Automatic Identity Server and MCP settings

### **Health Monitoring**
- **Liveness Probes:** Ensure containers are running
- **Readiness Probes:** Ensure containers are ready for traffic
- **Log Analytics:** Centralized logging and monitoring

## ⚡ Key Features

### **Auto-Scaling**
```bicep
scale: {
  minReplicas: 1
  maxReplicas: 10
  rules: [
    {
      name: 'http-scaling'
      http: {
        metadata: {
          concurrentRequests: '30'  // Scale up at 30 concurrent requests
        }
      }
    }
  ]
}
```

### **Health Checks**
```bicep
probes: [
  {
    type: 'Liveness'
    httpGet: {
      path: '/health'
      port: 9080
    }
    initialDelaySeconds: 30
    periodSeconds: 10
  }
]
```

### **Secret Management**
- Secrets stored securely in Container Apps
- Referenced from GitHub secrets during deployment
- No secrets in code or configuration files

## 🛠️ Customization

### **Environment-Specific Configuration**

**Staging (`staging.parameters.json`):**
```json
{
  "environmentName": { "value": "staging" },
  "clientId": { "value": "amlink-submissions-client-staging" },
  "submissionApiUrl": { "value": "https://api-staging.example.com/submissions" }
}
```

**Production (`production.parameters.json`):**
```json
{
  "environmentName": { "value": "prod" },
  "clientId": { "value": "amlink-submissions-client" },
  "submissionApiUrl": { "value": "https://api.example.com/submissions" }
}
```

### **Resource Sizing**

**Development/Staging:**
- CPU: 0.5 cores per replica
- Memory: 1Gi per replica
- Min replicas: 1
- Max replicas: 5

**Production:**
- CPU: 1.0 cores per replica (modify in `main.bicep`)
- Memory: 2Gi per replica
- Min replicas: 2
- Max replicas: 20

## 📊 Monitoring & Troubleshooting

### **Azure Portal**
1. Navigate to your resource group
2. View Container Apps for deployment status
3. Check Log Analytics for application logs
4. Monitor scaling and performance metrics

### **Application Logs**
```bash
# View logs via Azure CLI
az containerapp logs show \
  --name ca-amlink-submissions-mcp-staging-server \
  --resource-group rg-amlink-submissions-mcp-staging \
  --follow
```

### **Common Issues**

**Image Pull Errors:**
- Verify GitHub token has packages:read permission
- Ensure container images are published
- Check registry credentials in secrets

**Health Check Failures:**
- Verify `/health` endpoints are implemented
- Check application startup time vs probe delays
- Review application logs for errors

**Scaling Issues:**
- Monitor CPU/memory usage in Azure Portal
- Adjust scaling rules in `main.bicep`
- Verify health probes are passing

## 🔒 Security Considerations

### **Network Security**
- HTTPS enforced for all external traffic
- Internal communication between apps
- No direct internet access to containers

### **Secret Management**
- All secrets stored in Azure Key Vault integration
- No secrets in container images or logs
- Rotation supported via GitHub Actions

### **Container Security**
- Base images scanned for vulnerabilities
- Running as non-root user
- Minimal attack surface

## 💰 Cost Optimization

### **Pricing Model**
- **Pay per use:** Only pay for actual resource consumption
- **Auto-scaling:** Automatically scales down to minimum during low usage
- **No idle costs:** Unlike VMs, no costs when not processing requests

### **Cost-Saving Tips**
1. **Right-size resources:** Start with 0.5 CPU, 1Gi memory
2. **Optimize scaling:** Set appropriate min/max replicas
3. **Use staging sparingly:** Spin down staging when not testing
4. **Monitor usage:** Use Azure Cost Management tools

## 🚀 Advanced Scenarios

### **Custom Domains**
```bash
# Add custom domain to Container App
az containerapp hostname add \
  --hostname "mcp.yourdomain.com" \
  --name ca-amlink-submissions-mcp-prod-client \
  --resource-group rg-amlink-submissions-mcp-prod
```

### **Blue-Green Deployments**
- Use Container Apps revisions
- Split traffic between versions
- Instant rollback capability

### **Multi-Region Deployment**
- Deploy to multiple Azure regions
- Use Azure Front Door for global load balancing
- Configure geo-replication for data

---

## 📝 Next Steps

1. **Setup Azure Service Principal** and GitHub secrets
2. **Test staging deployment** with a pre-release tag
3. **Validate functionality** using deployment validation workflow
4. **Deploy to production** with a stable release
5. **Configure monitoring** and alerting
6. **Set up custom domains** (optional)

This deployment pipeline provides a robust, scalable, and cost-effective way to run your AmLink Submissions MCP in Azure! 🚀