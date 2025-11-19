# Container Registry Deployment

This directory contains workflows and configurations for building, publishing, and deploying container images.

## 🚀 Quick Start

### 1. Build and Push Images

**Option A: Manual Trigger (Recommended)**
```bash
# Go to GitHub Actions → "Build and Push Container Images" → "Run workflow"
# Choose your registry and tag, then click "Run workflow"
```

**Option B: Automatic on Release**
```bash
# Create and push a version tag
git tag v1.0.0
git push origin v1.0.0
```

### 2. Deploy Using Published Images

**Using PowerShell (Windows):**
```powershell
# Navigate to project root
cd C:\Temp\repos\amlink-submissions-mcp

# Deploy with default settings
.\scripts\deploy.ps1 deploy

# Deploy with specific tag
.\scripts\deploy.ps1 deploy -Tag "v1.0.0"
```

**Using Bash (Linux/Mac):**
```bash
# Make script executable
chmod +x scripts/deploy.sh

# Deploy with default settings
./scripts/deploy.sh deploy

# Deploy with specific tag
./scripts/deploy.sh deploy --tag v1.0.0
```

## 📋 Available Workflows

### 🔄 Workflow Separation Strategy

**CI Pipeline** (`ci-cd.yml`):
- ✅ **Code Validation**: Build, test, lint, security scan
- ✅ **Quality Gates**: Ensures code meets standards
- ✅ **Docker Build Validation**: Verifies images can be built
- ❌ **No Publishing**: Keeps CI fast and focused

**Build & Push** (`build-and-push.yml`):
- ✅ **CI Dependency**: Requires CI to pass first
- ✅ **Image Publishing**: Builds and pushes to registry
- ✅ **Image Security**: Scans published container images
- ✅ **Deployment Ready**: Creates deployable artifacts

### Build and Push Container Images
**File:** `.github/workflows/build-and-push.yml`

**Triggers:**
- ✅ Manual workflow dispatch (with registry and tag options)
- ✅ Git tags starting with `v*` (e.g., `v1.0.0`)
- ✅ Published releases

**Prerequisites:**
- 🔍 **CI Status Check**: Verifies main CI pipeline passed
- ⚠️ **Override Option**: Can skip CI check (not recommended)

**Features:**
- 🐳 Multi-architecture builds (AMD64, ARM64)
- 🔒 Container image security scanning with Trivy
- 📦 Supports GitHub Container Registry (ghcr.io) and Docker Hub
- 🏷️ Intelligent tagging (latest, branch, tag, SHA)
- ⚡ Build cache optimization
- 🚦 CI validation before publishing

### Registry-based Deployment
**File:** `docker-compose.registry.yml`

Uses pre-built images from the container registry instead of building locally.

## 🔧 Configuration

### Environment Setup

1. **Copy the environment template:**
   ```bash
   cp .env.prod.example .env.prod
   ```

2. **Edit `.env.prod` with your values:**
   ```env
   CERT_PASSWORD=your-certificate-password
   CLIENT_SECRET=your-secure-client-secret
   SUBMISSION_API_URL=https://your-api.com/submissions
   SUBMISSION_API_KEY=your-api-key
   ```

### Container Registry Options

**GitHub Container Registry (Default):**
```yaml
REGISTRY=ghcr.io
# Uses GitHub token automatically
```

**Docker Hub:**
```yaml
REGISTRY=docker.io
# Requires DOCKER_USERNAME and DOCKER_PASSWORD secrets
```

## 📊 Image Information

### Published Images
- **Client:** `ghcr.io/eduardomb-aw/amlink-submissions-mcp-client`
- **Server:** `ghcr.io/eduardomb-aw/amlink-submissions-mcp-server`

### Available Tags
- `latest` - Latest main branch build
- `v1.0.0` - Specific version tags
- `main` - Main branch builds
- `sha-abc1234` - Commit-specific builds

## 🛠️ Management Commands

### Deployment Management
```bash
# Deploy application
./scripts/deploy.sh deploy

# Check status
./scripts/deploy.sh status

# View logs
./scripts/deploy.sh logs

# Stop application
./scripts/deploy.sh stop

# Complete cleanup
./scripts/deploy.sh cleanup
```

### Manual Docker Commands
```bash
# Pull specific images
docker pull ghcr.io/eduardomb-aw/amlink-submissions-mcp-client:latest
docker pull ghcr.io/eduardomb-aw/amlink-submissions-mcp-server:latest

# Run with registry compose
docker compose -f docker-compose.registry.yml --env-file .env.prod up -d

# Stop services
docker compose -f docker-compose.registry.yml down
```

## 🔒 Security

### Image Scanning
- **Trivy vulnerability scanning** runs automatically after image builds
- **Results uploaded** to GitHub Security tab
- **SARIF format** for integration with security tools

### Certificate Management
- **Development certificates** included for local testing
- **Production certificates** should be provided in `./certs/` directory
- **Certificate password** configured via `CERT_PASSWORD` environment variable

## 🚦 Monitoring

### Health Checks
Both containers include health check endpoints:
- **Client:** `http://localhost:8080/health`
- **Server:** `http://localhost:9080/health`

### Service URLs
- **Client (HTTPS):** https://localhost:8443
- **Client (HTTP):** http://localhost:8080
- **Server (HTTPS):** https://localhost:9443
- **Server (HTTP):** http://localhost:9080

## 🔧 Troubleshooting

### Common Issues

**1. Images not found:**
```bash
# Ensure images are published
# Check GitHub Actions → "Build and Push Container Images"
```

**2. Certificate errors:**
```bash
# Verify certificates exist
ls -la certs/
# Check certificate password in .env.prod
```

**3. Permission errors:**
```bash
# Linux/Mac: Ensure script is executable
chmod +x scripts/deploy.sh
```

### Useful Commands
```bash
# Check running containers
docker ps

# View container logs
docker logs amlink-client-prod
docker logs amlink-server-prod

# Inspect container configuration
docker inspect amlink-client-prod
```

## 📈 Next Steps

1. **Set up monitoring** with your preferred solution
2. **Configure reverse proxy** (nginx, traefik) for production
3. **Set up backup strategies** for persistent data
4. **Implement log aggregation** (ELK, Splunk, etc.)
5. **Add performance monitoring** (APM tools)

---

For development deployment, see the main [README.md](../README.md) file.