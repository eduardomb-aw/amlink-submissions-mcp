# Development Workflow

This document outlines the development workflow for the AmLink Submissions MCP project.

## 🔄 Git Workflow

### Branch Strategy
- **`main`**: Production-ready code
- **`develop`**: Integration branch for features
- **`feature/*`**: Feature development branches
- **`hotfix/*`**: Critical production fixes
- **`release/*`**: Release preparation branches

### Workflow Steps
1. **Create Feature Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Development**
   ```bash
   # Make changes
   git add .
   git commit -m "feat: add new feature"
   ```

3. **Push and Create PR**
   ```bash
   git push origin feature/your-feature-name
   # Create PR via GitHub UI or CLI
   ```

4. **Code Review & Merge**
   - PR validation runs automatically
   - Require at least 1 approval
   - Merge to `develop` first, then `main`

## 🚀 CI/CD Pipeline Overview

### Triggers
- **Push to `main`**: Full deployment pipeline
- **Push to `develop`**: Build and test only
- **Pull Requests**: Validation and testing
- **Schedule**: Weekly dependency updates

### Pipeline Stages

1. **🧪 Test & Build**
   - Restore dependencies
   - Build solution
   - Run unit tests
   - Generate coverage reports

2. **🔒 Security Scan**
   - Vulnerability scanning with Trivy
   - Secret detection with TruffleHog
   - Code quality checks

3. **🐳 Build & Push Images**
   - Build Docker images
   - Push to GitHub Container Registry
   - Tag with version and latest

4. **🚀 Deploy**
   - **Staging**: Automatic deployment
   - **Production**: Manual approval required

## 🔧 Local Development Setup

### Prerequisites
```bash
# Install required tools
winget install Microsoft.DotNet.SDK.Preview  # .NET 10.0
winget install Docker.DockerDesktop
winget install Git.Git
```

### Environment Setup
```bash
# Clone repository
git clone https://github.com/eduardomb-aw/amlink-submissions-mcp.git
cd amlink-submissions-mcp

# Copy environment template
cp .env.example .env
# Edit .env with your values

# Generate HTTPS certificates
dotnet dev-certs https -ep ~/.aspnet/https/aspnetapp.pfx -p "YourSecurePassword123!"
dotnet dev-certs https --trust
```

### Running Locally
```bash
# Development mode (hot reload)
docker-compose up -d

# View logs
docker-compose logs -f

# Rebuild after changes
docker-compose up -d --build

# Stop services
docker-compose down
```

### Testing
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Format code
dotnet format
```

## 📦 Container Registry

Images are published to GitHub Container Registry:
- **Server**: `ghcr.io/eduardomb-aw/amlink-submissions-mcp-server`
- **Client**: `ghcr.io/eduardomb-aw/amlink-submissions-mcp-client`

### Image Tags
- `latest`: Latest stable release
- `main`: Latest from main branch
- `develop`: Latest from develop branch
- `v*.*.*`: Semantic version releases

## 🏗️ Deployment Environments

### Staging Environment
- **Client**: `https://app-amlink-submissions-mcp-staging-client.azurewebsites.net`
- **Server**: `https://app-amlink-submissions-mcp-staging-server.azurewebsites.net`
- **Purpose**: Integration testing and validation
- **Auto-deploy**: On releases

### Production Environment
- **Status**: Not yet provisioned
- **Purpose**: Live production environment
- **Deploy**: Manual approval required

## 🔍 Monitoring & Observability

### Health Checks
- `/health`: Application health status
- `/ready`: Readiness probe for Kubernetes

### Logging
- Structured logging with Serilog
- Centralized log aggregation
- Security audit trails

### Metrics
- Application performance metrics
- Container resource usage
- Business metrics (API calls, errors)

## 🚨 Incident Response

### Alerts
- High error rates
- Performance degradation
- Security events
- Infrastructure issues

### Rollback Procedure
```bash
# Quick rollback to previous version
docker-compose down
docker pull ghcr.io/eduardomb-aw/amlink-submissions-mcp:previous
docker-compose up -d
```

## 📋 Release Process

1. **Create Release Branch**
   ```bash
   git checkout -b release/v1.0.0
   ```

2. **Update Version Numbers**
   - Update `*.csproj` files
   - Update Docker tags
   - Update documentation

3. **Create Release PR**
   - Merge to `main`
   - Tag release
   - Deploy to production

4. **Post-Release**
   - Monitor deployment
   - Update documentation
   - Communicate changes