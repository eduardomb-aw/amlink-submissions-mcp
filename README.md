# AmLink Submissions MCP

<!-- Build & CI/CD Status -->
[![CI/CD Pipeline](https://github.com/eduardomb-aw/amlink-submissions-mcp/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/eduardomb-aw/amlink-submissions-mcp/actions/workflows/ci-cd.yml)
[![PR Validation](https://github.com/eduardomb-aw/amlink-submissions-mcp/actions/workflows/pr-validation.yml/badge.svg)](https://github.com/eduardomb-aw/amlink-submissions-mcp/actions/workflows/pr-validation.yml)
[![Build and Push](https://github.com/eduardomb-aw/amlink-submissions-mcp/actions/workflows/build-and-push.yml/badge.svg)](https://github.com/eduardomb-aw/amlink-submissions-mcp/actions/workflows/build-and-push.yml)

<!-- Code Quality -->
[![codecov](https://codecov.io/gh/eduardomb-aw/amlink-submissions-mcp/graph/badge.svg)](https://codecov.io/gh/eduardomb-aw/amlink-submissions-mcp)
[![Security: Trivy](https://img.shields.io/badge/Security-Trivy-blue)](https://github.com/eduardomb-aw/amlink-submissions-mcp/security/code-scanning)

<!-- Project Information -->
[![License: MIT](https://img.shields.io/github/license/eduardomb-aw/amlink-submissions-mcp)](https://github.com/eduardomb-aw/amlink-submissions-mcp/blob/main/LICENSE)
[![Latest Release](https://img.shields.io/github/v/release/eduardomb-aw/amlink-submissions-mcp)](https://github.com/eduardomb-aw/amlink-submissions-mcp/releases/latest)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Docker](https://img.shields.io/badge/Docker-GHCR-blue?logo=docker)](https://github.com/eduardomb-aw/amlink-submissions-mcp/pkgs/container/amlink-submissions-mcp-server)

<!-- Activity & Stats -->
[![GitHub last commit](https://img.shields.io/github/last-commit/eduardomb-aw/amlink-submissions-mcp)](https://github.com/eduardomb-aw/amlink-submissions-mcp/commits/main)
[![GitHub issues](https://img.shields.io/github/issues/eduardomb-aw/amlink-submissions-mcp)](https://github.com/eduardomb-aw/amlink-submissions-mcp/issues)
[![GitHub pull requests](https://img.shields.io/github/issues-pr/eduardomb-aw/amlink-submissions-mcp)](https://github.com/eduardomb-aw/amlink-submissions-mcp/pulls)

A Model Context Protocol (MCP) server and client implementation for AmLink
submissions API integration, built with ASP.NET Core and secured with
Identity Server 4.

## 🏗️ Architecture

- **MCP Server**: Provides MCP tools for interacting with AmLink Submission APIs
- **MCP Client**: Web interface for testing and demonstrating MCP functionality
- **Identity Server 4**: OAuth 2.0/OpenID Connect authentication
- **Docker**: Containerized deployment with development and production configurations

## 🚀 Quick Start

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Git](https://git-scm.com/)

### Development Setup

1. **Clone the repository**

   ```bash
   git clone https://github.com/your-username/amlink-submissions-mcp.git
   cd amlink-submissions-mcp
   ```

2. **Set up environment variables**

   ```bash
   cp .env.example .env
   # Edit .env with your actual values
   ```

3. **Generate HTTPS certificates (for HTTPS development)**

   ```bash
   dotnet dev-certs https -ep ~/.aspnet/https/aspnetapp.pfx -p "YourSecurePassword123!"
   dotnet dev-certs https --trust
   ```

4. **Start the services**

   ```bash
   # Development with debugging support (automatically loads docker-compose.override.yml)
   docker-compose up -d
   
   # Production build (no debugging)
   docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
   ```

5. **Access the applications**
   - **Client (HTTPS)**: <https://localhost:5001>
   - **Client (HTTP)**: <http://localhost:5000>
   - **Server (HTTPS)**: <https://localhost:8443>
   - **Server (HTTP)**: <http://localhost:8080>

### Debugging

Full VS Code debugging support is available for Docker containers:

```bash
# Start containers with debugging support
docker-compose up -d

# Attach VS Code debugger:
# 1. Open Command Palette (Ctrl+Shift+P)
# 2. Type "Docker: Attach Visual Studio Code"  
# 3. Select container: amlink-mcp-server or amlink-mcp-client
# 4. Set breakpoints and debug!
```

**Debug features:**
- Hot reload for code changes
- Full breakpoint debugging
- Variable inspection
- Source code mapping

📖 **Detailed debugging guide**: [DEBUG.md](DEBUG.md)

## 🔄 CI/CD & Deployment

### Workflow Separation

This project uses a **two-stage workflow** approach for optimal efficiency:

1. **🔍 CI Pipeline** (`ci-cd.yml`):
   - Validates code quality (build, test, security scan)
   - Runs on every push/PR for fast feedback
   - Does **not** publish artifacts (keeps CI fast)

2. **🐳 Build & Push** (`build-and-push.yml`):
   - Requires CI to pass first
   - Publishes container images to registry
   - Scans published images for vulnerabilities
   - Triggered manually or on releases

### Production Deployment

#### Option 1: From Registry (Recommended)

```bash
# 1. Publish images via GitHub Actions
# Go to Actions → "Build and Push Container Images" → Run workflow

# 2. Deploy using published images
.\scripts\deploy.ps1 deploy -Tag "latest"
```

#### Option 2: Local Build

```bash
# Build and deploy locally
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

📖 **Full deployment guide**: See [`docs/registry-deployment.md`](docs/registry-deployment.md)

- **Server (HTTP)**: <http://localhost:8080>

## 📁 Project Structure

```text
├── src/
│   ├── amlink-submissions-mcp-client/    # Web client application
│   └── amlink-submissions-mcp-server/    # MCP server implementation
├── docker-compose.yml                    # Base Docker configuration
├── docker-compose.override.yml           # Development overrides
├── docker-compose.prod.yml              # Production enhancements
├── .env.example                          # Environment template
└── docs/                                 # Additional documentation
```

## 🔧 Configuration

### Environment Variables

**Automatically Configured in Azure (via deployment pipeline):**

- `IdentityServer__*` - OAuth 2.0/OpenID Connect settings
- `McpServer__*` - MCP server URLs and configuration
- `ExternalApis__*` - AmLink API endpoints and settings
- `ASPNETCORE_*` - ASP.NET Core runtime settings

**Manual Configuration Required:**

| Variable | Description | Location |
|----------|-------------|----------|
| `OPENAI_API_KEY` | OpenAI API key for LLM integration | Azure Portal |
| `IDENTITY_SERVER_CLIENT_SECRET` | OAuth client secret | Azure Portal |

### Docker Compose Configurations

- **Base** (`docker-compose.yml`): Production-ready configuration
- **Override** (`docker-compose.override.yml`): Development settings with
  hot reload
- **Production** (`docker-compose.prod.yml`): Enhanced production features
  (health checks, resource limits)

## ⚙️ Deployment Configuration

### Automatic Environment Setup

The deployment pipeline automatically configures:

**Client Application:**

- Identity Server OAuth settings (client ID, scopes, redirect URIs)
- MCP server communication URLs
- Cross-app authentication configuration

**Server Application:**

- Identity Server authentication settings
- External API endpoints (AmLink Submission API)
- MCP server documentation URLs

**Infrastructure:**

- VNet integration with ArchPlayGroundAFRG-1 (East US 2)
- Subnet delegation for Web Apps connectivity
- Internal resource access configuration
- OAuth callback and response handling

**Manual Setup (Sensitive Data):**

- `OPENAI_API_KEY`: Set in Azure Portal → Web App → Configuration
- Optional OAuth secrets: Configure if custom authentication is needed

## 🔒 Security

- **HTTPS Support**: Self-signed certificates for development, configurable for production
- **OAuth 2.0**: Identity Server 4 integration for secure API access
- **Environment Isolation**: Separate configurations for development and production
- **Secret Management**: Environment variable-based configuration
- **Automatic Configuration**: Sensitive settings configured via deployment pipeline

## 📊 Observability & Monitoring

### Application Insights Integration

The project includes comprehensive Application Insights integration for distributed tracing and observability:

- **Automatic Correlation IDs**: Track requests across service boundaries
- **Distributed Tracing**: Visualize request flows through the system
- **Dependency Tracking**: Monitor external API calls (AmLink APIs, Identity Server)
- **Performance Metrics**: Track request duration, throughput, and response times
- **Exception Tracking**: Capture errors with full context and stack traces
- **Custom Telemetry**: Enhanced properties for filtering and analysis

**Key Features:**
- 90-day data retention in Application Insights
- Integration with Log Analytics workspace
- Custom telemetry processor for AmLink API tagging
- Live metrics stream for real-time monitoring
- Adaptive sampling for cost optimization

**Documentation:**
- [Application Insights Guide](./docs/APPLICATION-INSIGHTS.md) - Complete integration guide
- Configuration in `appsettings.json`
- Infrastructure provisioned in `infrastructure/main.bicep`

**Quick Start:**
```bash
# Connection string set automatically in Azure deployments
ConnectionStrings__ApplicationInsights="InstrumentationKey=...;IngestionEndpoint=..."

# Local development (telemetry disabled if not configured)
# Leave connection string empty in appsettings.json
```

## 🛠️ Development

### Code Formatting (REQUIRED)

**Before committing or creating PRs**, always check and fix code formatting:

```bash
# Option 1: Use the pre-commit validation script (recommended)
# Windows PowerShell
.\scripts\pre-commit-check.ps1

# Linux/macOS
./scripts/pre-commit-check.sh

# With automatic formatting fix
.\scripts\pre-commit-check.ps1 -FixFormatting

# Option 2: Manual validation
# Check formatting (required before any push)
dotnet format --verify-no-changes

# Fix formatting issues automatically
dotnet format

# Complete pre-commit validation
dotnet format --verify-no-changes && dotnet build --configuration Release && dotnet test --configuration Release
```

> **⚠️ Critical**: Formatting violations will cause PR validation failures. This step is mandatory.

### Building Locally

```bash
# Build client
dotnet build src/amlink-submissions-mcp-client/

# Build server  
dotnet build src/amlink-submissions-mcp-server/

# Run tests
dotnet test
```

### Docker Development

```bash
# Start with development configuration (auto-loaded)
docker-compose up -d

# View logs
docker-compose logs -f

# Rebuild containers
docker-compose up -d --build
```

## 🚢 Deployment

### Docker Compose Production

1. **Set production environment variables**
2. **Deploy with production configuration**

   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
   ```

### CI/CD Pipeline

This project includes GitHub Actions workflows for:

- ✅ Automated testing
- 🔨 Docker image building
- 🚀 Container registry publishing
- 📊 Security scanning

## 📝 API Documentation

- **MCP Server Tools**: Available at `/tools` endpoint
- **Health Checks**: Available at `/health`, `/health/ready`, `/health/live` endpoints
- **OpenAPI/Swagger**: Available in development mode

### Health Check Endpoints

Both the server and client applications provide health check endpoints for
monitoring and orchestration:

#### `/health` - Comprehensive Health Status

Returns detailed health information including all dependency checks:

```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "self",
      "status": "Healthy",
      "description": "Application is running",
      "duration": 0.5
    },
    {
      "name": "identity_server",
      "status": "Healthy",
      "description": null,
      "duration": 125.3
    },
    {
      "name": "submission_api",
      "status": "Healthy",
      "description": null,
      "duration": 98.7
    }
  ],
  "totalDuration": 224.5
}
```

#### `/health/ready` - Readiness Probe

Returns simple status indicating if the application is ready to accept
traffic:

```json
{
  "status": "Healthy"
}
```

#### `/health/live` - Liveness Probe

Returns simple status indicating if the application is running:

```json
{
  "status": "Healthy"
}
```

**Health Status Values:**

- `Healthy` - All checks passed
- `Degraded` - Some non-critical checks failed (e.g., external dependencies)
- `Unhealthy` - Critical checks failed

**Server Health Checks:**

- Self check - Application is running
- Identity Server - OAuth provider availability
- Submission API - External API availability

**Client Health Checks:**

- Self check - Application is running
- MCP Server - Backend server availability
- Identity Server - OAuth provider availability

## 🤝 Contributing

We welcome contributions! Here's how to get started:

### For New Contributors

1. **Review planned improvements**: See [TASKS.md](TASKS.md) for prioritized tasks
2. **Read the implementation guide**: Check [temp_task-implementation-guide.md](docs/temp_task-implementation-guide.md)
3. **Choose a task**: Pick from [POTENTIAL-IMPROVEMENTS.md](docs/POTENTIAL-IMPROVEMENTS.md)

### Development Workflow

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes following our [coding standards](.github/copilot-instructions.md)
4. Add tests for your changes
5. **MANDATORY**: Check code formatting before committing:
   ```bash
   # Use the pre-commit validation script (recommended)
   .\scripts\pre-commit-check.ps1 -FixFormatting
   
   # Or manual validation
   dotnet format --verify-no-changes
   dotnet format  # if issues found
   dotnet test --configuration Release
   ```
6. **Run comprehensive pre-push validation** (see below)
7. Commit your changes (`git commit -m 'Add amazing feature'`)
8. Push to the branch (`git push origin feature/amazing-feature`)
9. Open a Pull Request

### Pre-Push Validation

Before pushing any changes, **always** run our comprehensive pre-push validation to catch issues locally:

```powershell
# One-time setup (installs required tools)
.\scripts\setup-validation-tools.ps1

# Before every push (mirrors GitHub Actions exactly)
.\scripts\pre-push-validation.ps1
```

This prevents PR validation failures and provides immediate feedback in 30-60 seconds locally.

> **⚠️ Important**: PRs with formatting violations will fail validation. Always run `dotnet format --verify-no-changes` before pushing.

### Documentation

- **[TASKS.md](TASKS.md)** - Prioritized improvement tasks with detailed
  specifications
- **[POTENTIAL-IMPROVEMENTS.md](docs/POTENTIAL-IMPROVEMENTS.md)** -
  Comprehensive analysis of improvement opportunities
- **[temp_task-implementation-guide.md](docs/temp_task-implementation-guide.md)**
  \- Step-by-step guide for implementing tasks
- **[DEVELOPMENT.md](docs/DEVELOPMENT.md)** - Development setup and guidelines
- **[DEBUG.md](DEBUG.md)** - Comprehensive Docker debugging guide with VS Code integration

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE)
file for details.

## 🆘 Support

- 📧 **Issues**: Use GitHub Issues for bug reports and feature requests
- 📚 **Documentation**: Check the `/docs` folder for detailed guides
- 💬 **Discussions**: Use GitHub Discussions for questions and community
  support
