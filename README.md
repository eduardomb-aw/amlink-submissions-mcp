# AmLink Submissions MCP

A Model Context Protocol (MCP) server and client implementation for AmLink submissions API integration, built with ASP.NET Core and secured with Identity Server 4.

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
   # Development with hot reload
   docker-compose up -d
   
   # Production build
   docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
   ```

5. **Access the applications**
   - **Client (HTTPS)**: https://localhost:5001
   - **Client (HTTP)**: http://localhost:5000
   - **Server (HTTPS)**: https://localhost:8443

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

**Option 1: From Registry (Recommended)**
```bash
# 1. Publish images via GitHub Actions
# Go to Actions → "Build and Push Container Images" → Run workflow

# 2. Deploy using published images
.\scripts\deploy.ps1 deploy -Tag "latest"
```

**Option 2: Local Build**
```bash
# Build and deploy locally
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

📖 **Full deployment guide**: See [`docs/registry-deployment.md`](docs/registry-deployment.md)
   - **Server (HTTP)**: http://localhost:8080

## 📁 Project Structure

```
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
| `OPENAI_API_KEY` | OpenAI API key for LLM integration | Azure Portal (both apps) |
| `IDENTITY_SERVER_CLIENT_SECRET` | OAuth client secret (if needed) | Azure Portal (optional) |

### Docker Compose Configurations

- **Base** (`docker-compose.yml`): Production-ready configuration
- **Override** (`docker-compose.override.yml`): Development settings with hot reload
- **Production** (`docker-compose.prod.yml`): Enhanced production features (health checks, resource limits)

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

## 🛠️ Development

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

### Production Deployment

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
- **Health Checks**: Available at `/health` endpoint
- **OpenAPI/Swagger**: Available in development mode

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
5. Commit your changes (`git commit -m 'Add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

### Documentation

- **[TASKS.md](TASKS.md)** - Prioritized improvement tasks with detailed specifications
- **[POTENTIAL-IMPROVEMENTS.md](docs/POTENTIAL-IMPROVEMENTS.md)** - Comprehensive analysis of improvement opportunities
- **[temp_task-implementation-guide.md](docs/temp_task-implementation-guide.md)** - Step-by-step guide for implementing tasks
- **[DEVELOPMENT.md](docs/DEVELOPMENT.md)** - Development setup and guidelines

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- 📧 **Issues**: Use GitHub Issues for bug reports and feature requests
- 📚 **Documentation**: Check the `/docs` folder for detailed guides
- 💬 **Discussions**: Use GitHub Discussions for questions and community support