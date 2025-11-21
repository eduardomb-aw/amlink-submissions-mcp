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

## Test-Driven Development (TDD) Guidelines

### TDD Philosophy
This project follows strict Test-Driven Development practices with a clear RED-GREEN-REFACTOR cycle. Tests describe IDEAL behavior first, then implementation follows.

### TDD Process
1. **RED Phase**: Write failing tests that describe desired behavior (tests fail in PR, never in main)
2. **GREEN Phase**: Implement minimal code to make tests pass
3. **REFACTOR Phase**: Clean up code while keeping tests green
4. **MERGE**: All tests must pass before merging to main

### TDD Workflow Strategy
- **Feature Branches**: Create failing tests that will fail PR validation
- **Implementation**: Fix the failing tests through implementation and refactoring
- **Main Branch Protection**: Never merge failing tests into main branch
- **PR Validation**: Use failing tests as a forcing function for complete implementation

### Test Organization

#### RED Phase Tests (Active Failing Tests)
- Write tests WITHOUT `Skip` attribute - let them fail in PR validation
- Failing tests serve as a TODO list and prevent incomplete merges
- Focus on edge cases, error conditions, and expected behaviors
- Example pattern:
```csharp
[Fact] // No Skip - this WILL fail until implemented
public async Task GetSubmission_WithNullSubmissionId_ShouldThrowArgumentNullException()
{
    // Arrange - describe the scenario
    string? submissionId = null;

    // Act & Assert - define expected behavior
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(
        () => _submissionApiTools.GetSubmission(submissionId!));

    Assert.Equal("submissionId", exception.ParamName);
}
```

#### GREEN Phase Tests (Implementation Complete)
- All tests pass and PR validation succeeds
- Implementation satisfies all test requirements
- Code is refactored and clean while maintaining green tests

### Test Categories

#### Parameter Validation Tests
- Always test null, empty, and whitespace inputs
- Verify proper exception types and messages
- Test parameter validation BEFORE any HTTP calls or heavy operations

#### HTTP Integration Tests
- Mock `HttpMessageHandler` for external API calls
- Test request formation (method, URL, headers, body)
- Test various HTTP response scenarios (success, errors, timeouts)
- Validate proper error handling and exception types

#### JSON Processing Tests
- Test valid JSON responses
- Test invalid JSON handling
- Test edge cases like empty responses or unexpected formats

#### Security Tests
- Verify proper authorization header handling
- Test token validation and scope requirements
- Test unauthorized access scenarios

### Test Structure Standards

#### Arrange-Act-Assert Pattern
```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test data and mocks
    var input = "test-value";
    var expectedResult = "expected-output";
    
    // Act - Execute the method under test
    var result = await _service.Method(input);
    
    // Assert - Verify the behavior
    Assert.Equal(expectedResult, result);
}
```

#### Mock Setup Patterns
- Use `Mock<T>` for dependencies
- Set up mocks in constructor for reusability
- Use `Mock.Protected()` for `HttpMessageHandler` testing
- Capture requests with callbacks for verification

### TDD Best Practices

#### Test Naming
- Use descriptive names: `MethodName_Scenario_ExpectedBehavior`
- Be specific about the test scenario
- Make expectations clear in the name

#### Test Coverage Requirements
- **Parameter Validation**: All public methods must validate inputs
- **Error Handling**: Test all possible error conditions
- **Happy Path**: Test successful execution scenarios
- **Edge Cases**: Test boundary conditions and unusual inputs

#### Implementation Guidelines
- Start with simplest failing test
- Write minimal code to make test pass
- Add complexity only when tests require it
- Never write production code without a failing test

### TDD Workflow for New Features

1. **Create Feature Branch**: Start from main branch
2. **Analyze Requirements**: Understand what the feature should do
3. **Write Failing Tests**: Create comprehensive test suite that WILL fail PR validation
4. **Commit Failing Tests**: Push to feature branch - PR validation will fail (expected)
5. **Implement Code**: Write minimal code to make tests pass
6. **Refactor**: Improve code quality while keeping tests green
7. **Validate**: Ensure all tests pass and PR validation succeeds
8. **Merge**: Only merge when all tests pass - main branch stays clean

### Branch Protection Strategy
- **Main Branch**: Always has passing tests - never merge failing tests
- **Feature Branches**: Can have failing tests during development
- **PR Validation**: Failing tests block merge until implementation is complete
- **Forcing Function**: Failing tests in PR ensure complete implementation before merge

### TDD Anti-Patterns to Avoid
- ❌ Writing tests after implementation
- ❌ Making tests pass by changing the test instead of the code
- ❌ Writing tests that don't actually test the intended behavior
- ❌ Skipping edge cases or error conditions
- ❌ Writing implementation code without a failing test

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

### GitHub Actions Workflow Issues

#### .NET Version Compatibility
- GitHub Actions runners may not immediately support the latest .NET versions
- Use multi-version setup with fallback support:
  ```yaml
  - name: Setup .NET
    uses: actions/setup-dotnet@v4
    with:
      dotnet-version: |
        9.0.x
        10.0.x
  ```
- When .NET 10.0 is unavailable, it falls back to 9.0.x for compatibility

#### Docker Compose Commands
- GitHub Actions uses modern Docker with `docker compose` (space) instead of `docker-compose` (hyphen)
- Always use: `docker compose -f docker-compose.yml config`
- Never use: `docker-compose -f docker-compose.yml config`

#### Test Results Configuration
- Use specific filenames for test results to ensure reliable artifact collection
- Correct pattern: `--logger "trx;LogFileName=test-results.trx"`
- Avoid duplicate `--results-directory` parameters in dotnet test commands
- Structure test commands as:
  ```bash
  dotnet test --no-build --configuration Release \
    --collect:"XPlat Code Coverage" \
    --results-directory ./test-results \
    --logger "trx;LogFileName=test-results.trx"
  ```

#### Super-Linter Configuration
- Avoid mixing `VALIDATE_*: true` and `VALIDATE_*: false` settings
- Either include only linters you want (all true) or exclude specific ones (all false)
- Safe minimal configuration:
  ```yaml
  env:
    DEFAULT_BRANCH: main
    GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
    VALIDATE_DOCKERFILE_HADOLINT: true
    VALIDATE_YAML: true
    VALIDATE_JSON: true
    VALIDATE_MARKDOWN: true
    VALIDATE_ALL_CODEBASE: false
  ```

#### Merge Conflict Resolution
- Always fetch latest changes before resolving conflicts: `git fetch origin`
- Merge main into feature branch: `git merge origin/main`
- Resolve conflicts by choosing appropriate sections (keep structural improvements)
- Test workflow changes locally when possible before pushing
- After resolving conflicts, commit with descriptive message: `git commit -m "Resolve merge conflicts in [file]"`

#### Workflow Validation Best Practices
- Test dotnet commands locally before committing workflow changes
- Use `docker compose config` to validate compose files locally
- Check workflow syntax with VS Code YAML extension before committing
- Monitor workflow runs immediately after pushing changes
- Use `gh run list` and `gh run view --log-failed` for quick diagnostics

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

## Pull Request Workflow Troubleshooting

### Common PR Validation Failures and Solutions

#### Issue: "Behavior not supported, please either only include (VALIDATE=true) or exclude (VALIDATE=false) linters"
**Solution**: Simplify Super-Linter configuration by removing conflicting validation settings. Use only positive validation flags.

#### Issue: "Option '--results-directory' expects a single argument but 2 were provided"
**Solution**: Use `--results-directory` only once in dotnet test commands. Separate coverage and test results handling.

#### Issue: "docker-compose: command not found"
**Solution**: Replace `docker-compose` with `docker compose` (space instead of hyphen) in workflow files.

#### Issue: "No such file or directory" for test results publishing
**Solution**: Use specific filenames (`test-results.trx`) instead of glob patterns (`*.trx`) for more reliable artifact detection.

#### Issue: "required variable IDENTITY_SERVER_CLIENT_SECRET is missing a value"
**Solution**: Docker Compose validation fails when required environment variables aren't set. Provide dummy values for validation:
```yaml
- name: Validate Docker Compose
  env:
    IDENTITY_SERVER_CLIENT_SECRET: "validation-dummy-secret"
    OPENAI_API_KEY: "validation-dummy-key"
  run: |
    docker compose -f docker-compose.yml config
```

#### Issue: "Fix whitespace formatting" or "Process completed with exit code 2" in code formatting step
**Solution**: Code formatting violations detected by `dotnet format --verify-no-changes`. Fix formatting and commit:
```bash
# Fix all formatting issues automatically
dotnet format

# Check what files were changed
git status

# Add and commit the formatted files
git add [formatted-files]
git commit -m "Fix code formatting issues"
git push
```
**Common causes**: Mixed tabs/spaces, incorrect indentation, trailing whitespace, inconsistent line endings. Always run `dotnet format` locally before pushing changes.

### PR Merge Conflict Resolution Workflow
1. **Switch to feature branch**: `git checkout feature-branch`
2. **Fetch latest changes**: `git fetch origin`
3. **Merge main branch**: `git merge origin/main`
4. **Resolve conflicts** by editing conflicted files
5. **Add resolved files**: `git add [conflicted-files]`
6. **Complete merge**: `git commit -m "Resolve merge conflicts in [description]"`
7. **Push changes**: `git push`
8. **Monitor new workflow runs**: `gh run list --limit 3`

### Quick Diagnostic Commands
```bash
# Check recent workflow runs
gh run list --repo owner/repo --limit 5

# View failed run details
gh run view [run-id] --log-failed --repo owner/repo

# Re-run failed workflow
gh run rerun [run-id] --repo owner/repo

# Check PR status
gh pr view [pr-number] --repo owner/repo
```

## Best Practices

1. **Minimal Changes**: Make the smallest possible changes to achieve the goal
2. **Test First**: Write or update tests before implementing features
3. **Security**: Never commit secrets; always use environment variables
4. **Documentation**: Update documentation when changing public APIs or workflows
5. **Code Review**: Follow the repository's PR review process
6. **Dependencies**: Only add dependencies when absolutely necessary
7. **Docker**: Use Docker Compose for local development and testing
8. **Workflow Validation**: Test workflow changes locally and monitor runs immediately after pushing
9. **Incremental Fixes**: When workflows fail, fix one issue at a time rather than making multiple changes simultaneously
10. **Learning Documentation**: Update instructions based on troubleshooting experiences to prevent future issues
