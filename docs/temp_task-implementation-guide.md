# Task Implementation Guide

This guide helps contributors implement the tasks identified in [TASKS.md](../TASKS.md) and [temp_potential-improvements.md](temp_potential-improvements.md).

## Quick Start

1. **Choose a task** from [TASKS.md](../TASKS.md) based on priority and dependencies
2. **Create a feature branch** using the naming convention: `feature/task-{number}-{short-description}`
3. **Review the task details** including acceptance criteria and implementation guidelines
4. **Implement the task** following the repository's coding standards
5. **Test your changes** thoroughly (unit tests, integration tests, manual testing)
6. **Submit a PR** linking to the task/issue and requesting review

## Implementation Workflow

### 1. Pre-Implementation

- [ ] Read the full task description in TASKS.md
- [ ] Review related documentation (temp_potential-improvements.md)
- [ ] Check dependencies - ensure prerequisite tasks are complete
- [ ] Understand acceptance criteria
- [ ] Ask questions if anything is unclear (GitHub Discussions)

### 2. Development

- [ ] Create feature branch: `git checkout -b feature/task-{number}-{description}`
- [ ] Make small, incremental commits with clear messages
- [ ] Follow existing code patterns and conventions
- [ ] Add/update tests as you go
- [ ] Run builds and tests frequently: `dotnet build && dotnet test`
- [ ] Keep changes minimal and focused on the task

### 3. Testing

- [ ] Write unit tests for new code
- [ ] Write integration tests for workflows
- [ ] Run all tests: `dotnet test --verbosity normal`
- [ ] Test edge cases and error conditions
- [ ] Verify no regression in existing functionality
- [ ] Test with Docker Compose if infrastructure changes made

### 4. Documentation

- [ ] Update code comments where necessary
- [ ] Update README.md if public API changes
- [ ] Update relevant documentation in `docs/`
- [ ] Add inline documentation for complex logic
- [ ] Update TASKS.md to mark task as complete

### 5. Submission

- [ ] Run final build and tests: `dotnet build && dotnet test`
- [ ] Review all changes: `git diff main`
- [ ] Push branch: `git push origin feature/task-{number}-{description}`
- [ ] Create PR with:
  - Clear title referencing task/issue
  - Description of changes
  - Testing performed
  - Screenshots (if UI changes)
  - Link to task in TASKS.md
- [ ] Request review from maintainers
- [ ] Address review feedback

## Task-Specific Guidelines

### Phase 1: Foundation Tasks

#### Task 1: Comprehensive Test Suite

**Key Points:**

- Start with unit tests before integration tests
- Use xUnit as the testing framework (already configured)
- Mock external dependencies (Identity Server, Submission API)
- Aim for ≥80% coverage overall, ≥90% for critical code

**Tools:**

```bash
# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# View coverage report (after adding coverlet)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:coverage.opencover.xml -targetdir:coverage-report
```

**Test Structure:**

```csharp
public class SubmissionApiToolsTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<SubmissionApiTools>> _loggerMock;
    private readonly SubmissionApiTools _sut;

    public SubmissionApiToolsTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<SubmissionApiTools>>();
        _sut = new SubmissionApiTools(_httpClientFactoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetSubmission_WithValidId_ReturnsSubmissionDetails()
    {
        // Arrange
        var submissionId = "123";
        // ... setup mocks

        // Act
        var result = await _sut.GetSubmission(submissionId);

        // Assert
        Assert.NotNull(result);
        // ... additional assertions
    }
}
```

#### Task 2: Input Validation

**Key Points:**

- Add validation at the beginning of each method
- Use ArgumentException for invalid arguments
- Include parameter name in exception: `nameof(submissionId)`
- Write clear, actionable error messages
- Test validation with unit tests

**Validation Pattern:**

```csharp
public async Task<string> MethodName(string param1, int param2)
{
    // Validate parameters first
    if (string.IsNullOrWhiteSpace(param1))
        throw new ArgumentException("Parameter cannot be null or empty", nameof(param1));
    
    if (param2 <= 0)
        throw new ArgumentException("Parameter must be positive", nameof(param2));
    
    // Method implementation
    // ...
}
```

#### Task 3: Structured Logging

**Key Points:**

- Replace ALL Console.WriteLine with ILogger
- Use appropriate log levels (Information, Warning, Error)
- Use structured logging with semantic properties
- Don't log sensitive data (tokens, secrets, PII)

**Logging Pattern:**

```csharp
// Inject ILogger
private readonly ILogger<ClassName> _logger;

// Use structured logging
_logger.LogInformation("Processing submission {SubmissionId} for account {AccountId}", 
    submissionId, accountId);

// Log errors with exceptions
_logger.LogError(ex, "Failed to process submission {SubmissionId}", submissionId);
```

### Phase 2: Reliability Tasks

#### Task 4: JWT Token Handling

**Key Points:**

- Add NuGet package: `System.IdentityModel.Tokens.Jwt`
- Use JwtSecurityTokenHandler for parsing
- Validate token expiration
- Check for required claims
- Log validation failures

**Package Installation:**

```bash
cd src/amlink-submissions-mcp-server
dotnet add package System.IdentityModel.Tokens.Jwt
```

#### Task 5: HTTP Client Resilience

**Key Points:**

- Add NuGet package: `Microsoft.Extensions.Http.Resilience`
- Configure in DI container (Program.cs)
- Use IHttpClientFactory in services
- Configure retry, circuit breaker, and timeout policies

**Package Installation:**

```bash
cd src/amlink-submissions-mcp-server
dotnet add package Microsoft.Extensions.Http.Resilience
```

#### Task 6: Correlation IDs

**Key Points:**

- Create middleware to add X-Correlation-ID header
- Use LogContext.PushProperty for Serilog (if used)
- Include correlation ID in all log messages
- Pass correlation ID to downstream services

**Middleware Pattern:**

```csharp
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        
        context.Response.Headers.Add(CorrelationIdHeader, correlationId);
        
        // Store in HttpContext for use in services
        context.Items[CorrelationIdHeader] = correlationId;
        
        await _next(context);
    }
}
```

#### Task 7: Health Checks

**Key Points:**

- Add NuGet package: `Microsoft.Extensions.Diagnostics.HealthChecks`
- Check self, Identity Server, and Submission API
- Create multiple endpoints (health, ready, live)
- Return JSON with detailed status

**Package Installation:**

```bash
cd src/amlink-submissions-mcp-server
dotnet add package Microsoft.Extensions.Diagnostics.HealthChecks
dotnet add package AspNetCore.HealthChecks.Uris
```

### Phase 3: Quality Tasks

#### Task 8: EditorConfig

**Key Points:**

- Create .editorconfig at repository root
- Follow .NET conventions
- Enable analyzers in .csproj files
- Run `dotnet format` to fix existing issues

**Format Command:**

```bash
# Install dotnet-format if not already installed
dotnet tool install -g dotnet-format

# Format code
dotnet format
```

#### Task 9: Configuration Validation

**Key Points:**

- Create ConfigurationValidator class
- Call validation in Program.cs before building app
- Validate all required fields
- Check URL formats
- Validate enum values

#### Task 10: API Versioning

**Key Points:**

- This is primarily a documentation and planning task
- Choose versioning approach (tool names, metadata, or server version)
- Document the strategy
- Create migration guide for future versions

## Testing Best Practices

### Unit Test Guidelines

1. **Use AAA Pattern:** Arrange, Act, Assert
2. **One assertion per test:** Test one thing at a time
3. **Use descriptive names:** `MethodName_Scenario_ExpectedBehavior`
4. **Mock external dependencies:** Use Moq or NSubstitute
5. **Test edge cases:** Null, empty, invalid, boundary conditions

### Integration Test Guidelines

1. **Use WebApplicationFactory:** For testing ASP.NET Core apps
2. **Use test database:** Or in-memory database for data tests
3. **Clean up after tests:** Reset state between tests
4. **Test real workflows:** End-to-end scenarios
5. **Use TestContainers:** For testing with real dependencies (optional)

## Common Commands

```bash
# Build
dotnet build --configuration Release

# Run tests
dotnet test --verbosity normal

# Run tests with coverage
dotnet test /p:CollectCoverage=true

# Format code
dotnet format

# Restore packages
dotnet restore

# Clean build artifacts
dotnet clean

# Run in Docker
docker-compose up -d

# View Docker logs
docker-compose logs -f

# Stop Docker services
docker-compose down
```

## Code Style Guidelines

Follow the existing code style in the repository:

- **Naming:** PascalCase for classes/methods, camelCase for variables
- **Indentation:** 4 spaces (no tabs)
- **Braces:** Opening brace on new line (Allman style)
- **Using statements:** Inside namespace
- **Null checks:** Use null-conditional operators when appropriate
- **Async:** Always use async/await for I/O operations
- **Exceptions:** Use specific exception types, include messages

## Getting Help

- **GitHub Discussions:** Ask questions and discuss ideas
- **temp_potential-improvements.md:** Detailed analysis and recommendations
- **TASKS.md:** Complete task specifications
- **README.md:** Project overview and setup
- **Code comments:** Look at existing code for patterns

## PR Review Checklist

Before requesting review, ensure:

- [ ] All tests pass locally
- [ ] Code follows existing style
- [ ] New code has tests
- [ ] Documentation is updated
- [ ] Commit messages are clear
- [ ] No console output in production code (unless appropriate)
- [ ] No secrets or sensitive data committed
- [ ] Changes are minimal and focused
- [ ] PR description explains the changes

## Questions?

If you have questions about a task:

1. Review the task description in TASKS.md
2. Check temp_potential-improvements.md for context
3. Search existing issues and discussions
4. Ask in GitHub Discussions
5. Comment on the related issue

---

*Document created: 2025-11-21*  
*For: AmLink Submissions MCP Contributors*  
*Version: 1.0*
