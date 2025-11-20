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
- **Deployment**: `/docs/DEPLOYMENT.md`, `/docs/registry-deployment.md`
- **Development**: `/docs/DEVELOPMENT.md`
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

## Best Practices

1. **Minimal Changes**: Make the smallest possible changes to achieve the goal
2. **Test First**: Write or update tests before implementing features
3. **Security**: Never commit secrets; always use environment variables
4. **Documentation**: Update documentation when changing public APIs or workflows
5. **Code Review**: Follow the repository's PR review process
6. **Dependencies**: Only add dependencies when absolutely necessary
7. **Docker**: Use Docker Compose for local development and testing
