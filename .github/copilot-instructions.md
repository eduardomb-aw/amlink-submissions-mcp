# Copilot Instructions for AmLink Submissions MCP

## Project Overview

This is a Model Context Protocol (MCP) server and client implementation for AmLink submissions API integration. The project consists of:

- **MCP Server**: ASP.NET Core web API providing MCP tools for interacting with AmLink Submission APIs
- **MCP Client**: Razor Pages web application for testing and demonstrating MCP functionality
- **Identity Server 4**: OAuth 2.0/OpenID Connect authentication layer

## Technology Stack

- **Framework**: .NET 10.0
- **Web Framework**: ASP.NET Core (Razor Pages for client, Web API for server)
- **MCP Protocol**: ModelContextProtocol v0.4.0-preview.3
- **Authentication**: JWT Bearer tokens, Identity Server 4 (OAuth 2.0/OpenID Connect)
- **Containerization**: Docker with Docker Compose
- **Testing**: xUnit
- **CI/CD**: GitHub Actions

## Project Structure

```
├── src/
│   ├── amlink-submissions-mcp-client/    # Web client application (Razor Pages)
│   │   ├── Configuration/                # Client configuration classes
│   │   ├── Services/                     # MCP and Token services
│   │   ├── Model/                        # Data models
│   │   └── Pages/                        # Razor Pages
│   ├── amlink-submissions-mcp-server/    # MCP server implementation (Web API)
│   │   ├── Configuration/                # Server configuration classes
│   │   ├── Tools/                        # MCP tools implementation
│   │   └── Program.cs                    # Entry point
│   └── amlink-submissions-mcp.Tests/     # xUnit test project
├── .github/workflows/                     # CI/CD workflows
├── docker/                                # Docker-related files
├── docs/                                  # Documentation
├── infrastructure/                        # Infrastructure as code
└── scripts/                              # Deployment and utility scripts
```

## Build and Test

### Local Build
```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run tests
dotnet test --configuration Release
```

### Docker Build
```bash
# Development with hot reload
docker-compose up -d

# Production build
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### Running Tests
- All tests are located in `src/amlink-submissions-mcp.Tests/`
- Use xUnit test framework
- Run with: `dotnet test --verbosity normal`

## Coding Standards

### General Guidelines
- Use C# 12 features and .NET 10 idioms
- Enable nullable reference types (`<Nullable>enable</Nullable>`)
- Use implicit usings (`<ImplicitUsings>enable</ImplicitUsings>`)
- Follow ASP.NET Core conventions and patterns

### Naming Conventions
- Use PascalCase for classes, methods, and properties
- Use camelCase for local variables and parameters
- Use kebab-case for project names (e.g., `amlink-submissions-mcp-server`)
- Prefix interfaces with 'I' (e.g., `IMcpService`, `ITokenService`)

### Commit Messages
- Write succinct, clear commit messages
- Use imperative mood (e.g., "Add feature" not "Added feature")
- Keep the subject line under 50 characters when possible
- Focus on what and why, not how

### Code Organization
- Keep configuration classes in `Configuration/` folders
- Put service interfaces and implementations in `Services/` folders
- Store data models in `Model/` or `Models/` folders
- Place MCP tool implementations in `Tools/` folder

### Security
- Never commit secrets or API keys to the repository
- Use environment variables for sensitive configuration
- All configuration should reference `.env.example` patterns
- The server uses JWT Bearer authentication - ensure all endpoints are properly secured
- Identity Server 4 integration requires proper OAuth 2.0 flow implementation

## Environment Configuration

Required environment variables (see `.env.example`):
- `IDENTITY_SERVER_CLIENT_SECRET`: OAuth client secret for authentication
- `OPENAI_API_KEY`: OpenAI API key for LLM integration
- `ASPNETCORE_ENVIRONMENT`: Runtime environment (Development/Production)

## CI/CD Workflows

### Workflow Structure
1. **CI/CD Pipeline** (`ci-cd.yml`): Runs on every push/PR
   - Build and test validation
   - Security scanning with Trivy
   - Docker build validation (main branch only)

2. **Build and Push** (`build-and-push.yml`): Manual trigger for container publishing
   - Builds and pushes container images to registry
   - Includes vulnerability scanning of published images

3. **Other Workflows**: Additional workflows for releases, deployments, and dependency updates

### Making Changes
- All PRs must pass CI checks before merging
- Tests must pass for all changes
- Security scans must complete without critical vulnerabilities
- Docker builds must succeed for main branch changes

## Documentation

- **Main README**: `/README.md` - Quick start and overview
- **Deployment**: `/docs/deployment.md`, `/docs/registry-deployment.md`
- **Development**: `/docs/development.md`
- **Azure Deployment**: `/docs/azure-deployment.md`
- **Release Pipeline**: `/docs/release-pipeline.md`

## MCP-Specific Guidelines

### MCP Tools
- MCP tools are implemented in `src/amlink-submissions-mcp-server/Tools/`
- Tools should follow the ModelContextProtocol SDK patterns
- Use the `ModelContextProtocol.AspNetCore` package for integration

### MCP Server Configuration
- Server configuration is in `src/amlink-submissions-mcp-server/Configuration/`
- Ensure proper registration of MCP services in `Program.cs`
- Health checks should be available at `/health` endpoint

### MCP Client
- Client uses services to communicate with the MCP server
- Implements token-based authentication via `ITokenService`
- Uses `IMcpService` for MCP protocol interactions

## Common Tasks

### Adding a New MCP Tool
1. Create a new class in `src/amlink-submissions-mcp-server/Tools/`
2. Implement the tool following MCP SDK patterns
3. Register the tool in `Program.cs`
4. Add corresponding tests in the test project
5. Update documentation if needed

### Adding a New Service
1. Define an interface in the `Services/` folder (e.g., `IMyService.cs`)
2. Implement the interface in the same folder (e.g., `MyService.cs`)
3. Register the service in `Program.cs` using dependency injection
4. Add unit tests for the service

### Updating Dependencies
1. Update package references in `.csproj` files
2. Run `dotnet restore` to update dependencies
3. Run `dotnet build` to verify compatibility
4. Run `dotnet test` to ensure tests pass
5. Check for security vulnerabilities with Trivy or dependabot

### Making Documentation Changes
- Update relevant `.md` files in `/docs` or root directory
- Ensure consistency with README.md
- No build or test required for documentation-only changes

## Troubleshooting

### Build Failures
- Ensure .NET 10.0 SDK is installed
- Run `dotnet clean` followed by `dotnet restore`
- Check for missing environment variables

### Docker Issues
- Verify Docker Desktop is running
- Check `docker-compose.yml` and override files for configuration issues
- Ensure required environment variables are set in `.env` file
- Review logs with `docker-compose logs -f`

### Authentication Issues
- Verify `IDENTITY_SERVER_CLIENT_SECRET` is properly configured
- Check Identity Server 4 configuration in both client and server
- Ensure JWT Bearer tokens are correctly validated

## Docker Guidelines

### Critical Docker Rules
1. **Environment Variable Validation**: Always double-check that all necessary environment variables are defined in containers and have correct values, regardless of being simple variables, local secrets, or remote secrets
2. **Docker Compose Only**: Must always use Docker Compose to spin containers up or down - never use direct `docker run` commands
3. **Minimal Compose Files**: Must always maintain a minimum number of Docker Compose files with clear purposes
4. **Network Configuration**: Must always double-check that appropriate network configuration is in place for containers
5. **Alphabetical Environment Variables**: Environment variables must always be defined in alphabetical order for consistency and maintainability

### Required Environment Variables

#### MCP Server
- `ASPNETCORE_ENVIRONMENT` - Runtime environment (Development/Production)
- `ASPNETCORE_URLS` - Binding URLs for the server (e.g., `http://+:80;https://+:443`)
- `ASPNETCORE_Kestrel__Certificates__Default__Password` - SSL certificate password for HTTPS
- `ASPNETCORE_Kestrel__Certificates__Default__Path` - Path to SSL certificate file
- `ASPNETCORE_FORWARDEDHEADERS_ENABLED` - Enable forwarded headers (Production only)
- `ASPNETCORE_HTTPS_PORT` - HTTPS port for redirects (Production: `8443`)
- `Server__Url` - Internal server URL for container communication (e.g., `http://amlink-mcp-server:80`)
- `Server__ResourceBaseUri` - Base URI for server resources (optional)
- `Server__ResourceDocumentationUrl` - URL for API documentation (optional)
- `McpServer__BrowserUrl` - External server URL for browser access (e.g., `http://localhost:7072`)
- `IdentityServer__Url` - Identity Server base URL (e.g., `https://identitydev.amwins.com`)
- `IdentityServer__ClientId` - OAuth client identifier (e.g., `al-mcp-client`)
- `IdentityServer__ClientSecret` - OAuth client secret (REQUIRED - never use defaults)
- `IdentityServer__GrantType` - OAuth grant type (typically `authorization_code`)
- `IdentityServer__Scopes` - Required OAuth scopes (space-separated)
- `ExternalApis__SubmissionApi__BaseUrl` - AmLink Submission API base URL
- `ExternalApis__SubmissionApi__RequiredScope` - Required scope for submission API
- `ExternalApis__SubmissionApi__UserAgent` - User agent for API calls
- `ExternalApis__SubmissionApi__Version` - API version
- `OPENAI_API_KEY` - OpenAI API key for LLM integration (REQUIRED)

#### MCP Client
- `ASPNETCORE_ENVIRONMENT` - Runtime environment (Development/Production)
- `ASPNETCORE_URLS` - Binding URLs for the client (e.g., `http://+:80;https://+:443`)
- `ASPNETCORE_Kestrel__Certificates__Default__Password` - SSL certificate password for HTTPS
- `ASPNETCORE_Kestrel__Certificates__Default__Path` - Path to SSL certificate file
- `ASPNETCORE_FORWARDEDHEADERS_ENABLED` - Enable forwarded headers (Production only)
- `ASPNETCORE_HTTPS_PORT` - HTTPS port for redirects (Production: `5001`)
- `McpServer__Url` - Internal MCP server URL (container-to-container, e.g., `http://amlink-mcp-server:80`)
- `McpServer__BrowserUrl` - External MCP server URL (browser access, e.g., `http://localhost:8080`)
- `McpServer__Name` - Display name for MCP server (optional)
- `McpServer__TimeoutSeconds` - Timeout for MCP requests (default: 30)
- `IdentityServer__Url` - Identity Server base URL (e.g., `https://identitydev.amwins.com`)
- `IdentityServer__ClientId` - OAuth client identifier (e.g., `al-mcp-client`)
- `IdentityServer__ServerClientId` - Server client identifier (typically same as ClientId)
- `IdentityServer__ClientSecret` - OAuth client secret (REQUIRED - never use defaults)
- `IdentityServer__GrantType` - OAuth grant type (typically `authorization_code`)
- `IdentityServer__Scopes` - Required OAuth scopes (space-separated)
- `IdentityServer__RedirectUri` - OAuth callback URL (MUST match Identity Server config exactly)
- `IdentityServer__ResponseMode` - OAuth response mode (typically `query`)
- `DataProtection__KeyRingPath` - Path for data protection keys (Production: `/tmp/dp-keys`)
- `OPENAI_API_KEY` - OpenAI API key (REQUIRED)

#### External Environment Variables (Host System)
- `IDENTITY_SERVER_CLIENT_SECRET` - Set on host system, referenced in compose files
- `OPENAI_API_KEY` - Set on host system, referenced in compose files

#### Registry Deployment Variables (.env.prod.example)
- `CERT_PASSWORD` - Certificate password for production deployment
- `CLIENT_ID` - Production OAuth client ID
- `CLIENT_SECRET` - Production OAuth client secret
- `SUBMISSION_API_URL` - Production AmLink Submission API URL
- `SUBMISSION_API_KEY` - API key for submission service
- `CLIENT_IMAGE_TAG` - Docker image tag for client (avoid 'latest' in production)
- `SERVER_IMAGE_TAG` - Docker image tag for server (avoid 'latest' in production)
- `REGISTRY` - Docker registry URL (e.g., `ghcr.io`)

### Mandatory Docker Compose Commands

#### Starting Services
```bash
# Development (uses override file automatically)
docker-compose up -d

# Production
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# With rebuild
docker-compose up -d --build
```

#### Stopping Services
```bash
# Stop and remove containers
docker-compose down

# Clean restart
docker-compose down && docker-compose up -d
```

### Docker Compose File Structure
1. **`docker-compose.yml`** - Base production configuration (MANDATORY)
2. **`docker-compose.override.yml`** - Development overrides (auto-loaded)
3. **`docker-compose.prod.yml`** - Production-specific settings (optional)

### Network Configuration Requirements
- **Internal Communication**: Use service names (e.g., `http://amlink-mcp-server:80`)
- **External Access**: Use localhost with mapped ports (e.g., `https://localhost:5001`)
- **All services must be on the same Docker network**
- **Verify DNS resolution between containers**

### Pre-Deployment Validation Checklist
- [ ] All required environment variables are defined with real values
- [ ] Secret values are not using defaults or dummy data
- [ ] `docker-compose config` passes without errors
- [ ] Network allows required communication paths
- [ ] Port mappings don't conflict with host services
- [ ] OAuth redirect URIs match Identity Server configuration exactly

## Best Practices

1. **Minimal Changes**: Make the smallest possible changes to achieve the goal
2. **Test First**: Write or update tests before implementing features
3. **Security**: Never commit secrets; always use environment variables
4. **Documentation**: Update documentation when changing public APIs or workflows
5. **Code Review**: Follow the repository's PR review process
6. **Dependencies**: Only add dependencies when absolutely necessary
7. **Docker**: Use Docker Compose for local development and testing
