#!/bin/bash

# Script to create GitHub issues based on TASKS.md
# Usage: ./scripts/create-issues.sh

set -e

REPO="eduardomb-aw/amlink-submissions-mcp"
DOCS_DIR="docs"

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${BLUE}Creating GitHub Issues for AmLink Submissions MCP${NC}"
echo -e "${BLUE}=================================================${NC}\n"

# Check if gh is authenticated
if ! gh auth status &>/dev/null; then
    echo -e "${YELLOW}GitHub CLI is not authenticated. Please run: gh auth login${NC}"
    exit 1
fi

# Function to create an issue
create_issue() {
    local title="$1"
    local body_file="$2"
    local labels="$3"
    local milestone="$4"
    
    echo -e "${BLUE}Creating issue: ${title}${NC}"
    
    # Create the issue and capture the URL
    if [ -f "$body_file" ]; then
        issue_url=$(gh issue create \
            --repo "$REPO" \
            --title "$title" \
            --body-file "$body_file" \
            --label "$labels")
        echo -e "${GREEN}✓ Created: $issue_url${NC}\n"
    else
        echo -e "${YELLOW}⚠ Body file not found: $body_file${NC}\n"
    fi
}

# Create temporary directory for issue bodies
TEMP_DIR=$(mktemp -d)
trap "rm -rf $TEMP_DIR" EXIT

# Issue 3: Add Input Validation to All MCP Tools
cat > "$TEMP_DIR/issue-03.md" << 'EOF'
## Description

MCP tool methods in `SubmissionApiTools.cs` currently accept parameters without validation. This can lead to NullReferenceException, poor error messages, and potential security risks including injection attacks.

## Current Issues

- No null checks on `submissionId` in `GetSubmission()`
- No validation on `accountId` in `CreateSubmission()` and `ListSubmissions()`
- No validation on `submissionData` in `CreateSubmission()`
- Invalid inputs could cause runtime exceptions or API errors

## Proposed Solution

Add comprehensive input validation to all tool methods:

```csharp
public async Task<string> GetSubmission(string submissionId)
{
    if (string.IsNullOrWhiteSpace(submissionId))
        throw new ArgumentException("Submission ID cannot be null or empty", nameof(submissionId));
    
    // Method implementation...
}

public async Task<string> CreateSubmission(int accountId, string submissionData)
{
    if (accountId <= 0)
        throw new ArgumentException("Account ID must be a positive integer", nameof(accountId));
    
    if (string.IsNullOrWhiteSpace(submissionData))
        throw new ArgumentException("Submission data cannot be null or empty", nameof(submissionData));
    
    // Validate JSON format
    try
    {
        JsonDocument.Parse(submissionData);
    }
    catch (JsonException ex)
    {
        throw new ArgumentException("Submission data must be valid JSON", nameof(submissionData), ex);
    }
    
    // Method implementation...
}
```

## Acceptance Criteria

- [ ] All MCP tool methods validate their input parameters
- [ ] Proper ArgumentException thrown with descriptive messages
- [ ] Unit tests for validation edge cases
- [ ] Documentation updated with parameter requirements
- [ ] No breaking changes to existing valid calls

## Files to Modify

- `src/amlink-submissions-mcp-server/Tools/SubmissionApiTools.cs` (add validation to 4 methods)
- `src/amlink-submissions-mcp.Tests/Tools/SubmissionApiToolsTests.cs` (add validation tests)

## Dependencies

Should be implemented before or alongside the comprehensive test suite task.

## Related Documentation

- [TASKS.md - Task 2](../TASKS.md#task-2-add-input-validation-to-all-mcp-tools)
- [POTENTIAL-IMPROVEMENTS.md](docs/POTENTIAL-IMPROVEMENTS.md#2-missing-input-validation)

**Priority:** High  
**Estimated Effort:** 1-2 days
EOF

create_issue "Add Input Validation to All MCP Tools" "$TEMP_DIR/issue-03.md" "enhancement,security,validation,high-priority"

# Issue 4: Replace Console.WriteLine with Structured Logging
cat > "$TEMP_DIR/issue-04.md" << 'EOF'
## Description

There are 15 instances of `Console.WriteLine` in production code (primarily in `Program.cs` files) that should be replaced with structured logging using `ILogger`. This prevents proper log aggregation, filtering, and integration with monitoring tools like Application Insights.

## Current Issues

- Lost logging context (timestamps, log levels, correlation IDs)
- Cannot be captured by logging infrastructure
- Difficult to filter or search in production
- No integration with Application Insights, Serilog, etc.

## Proposed Solution

Replace all `Console.WriteLine` calls with structured `ILogger` calls:

```csharp
// Before
Console.WriteLine($"Starting MCP server with Identity Server 4 authorization at {serverConfig.Url}");

// After
logger.LogInformation("Starting MCP server with Identity Server 4 authorization at {ServerUrl}", serverConfig.Url);
```

Update `DisplayStartupInfo()` method to accept and use `ILogger<Program>`:

```csharp
static void DisplayStartupInfo(
    ServerConfiguration serverConfig, 
    IdentityServerConfiguration idsConfig, 
    ExternalApisConfiguration externalApisConfig,
    ILogger<Program> logger)
{
    logger.LogInformation("Starting MCP server with Identity Server 4 authorization at {ServerUrl}", serverConfig.Url);
    logger.LogInformation("Using Identity Server 4: {IdentityServerUrl}", idsConfig.Url);
    logger.LogInformation("Client ID: {ClientId}", idsConfig.ClientId);
    // ... etc
}
```

## Acceptance Criteria

- [ ] No `Console.WriteLine` statements in production code
- [ ] All logging uses `ILogger` with structured logging patterns
- [ ] Appropriate log levels used (Information, Warning, Error)
- [ ] Logs include semantic properties (structured data)
- [ ] Logs are properly captured in Application Insights or other log sinks
- [ ] Startup information is properly logged

## Files to Modify

- `src/amlink-submissions-mcp-server/Program.cs` (DisplayStartupInfo method)
- `src/amlink-submissions-mcp-client/Program.cs` (startup messages)

## Related Documentation

- [TASKS.md - Task 3](../TASKS.md#task-3-replace-consolewriteline-with-structured-logging)
- [POTENTIAL-IMPROVEMENTS.md](docs/POTENTIAL-IMPROVEMENTS.md#3-consolewriteline-instead-of-structured-logging)
- [ASP.NET Core Logging](https://learn.microsoft.com/aspnet/core/fundamentals/logging)

**Priority:** High  
**Estimated Effort:** 1 day
EOF

create_issue "Replace Console.WriteLine with Structured Logging" "$TEMP_DIR/issue-04.md" "enhancement,logging,observability,high-priority"

# Issue 5: Improve JWT Token Handling
cat > "$TEMP_DIR/issue-05.md" << 'EOF'
## Description

The current `TokenHasRequiredScope()` method uses manual base64 string parsing, which is error-prone and doesn't validate signatures or claims properly. This should be replaced with the standard `System.IdentityModel.Tokens.Jwt` library.

## Current Issues

- Manual JWT parsing is error-prone
- No signature validation
- Swallows all exceptions making debugging difficult
- Not following JWT best practices
- Potential security vulnerability

## Proposed Solution

1. Add NuGet package: `System.IdentityModel.Tokens.Jwt`
2. Rewrite `TokenHasRequiredScope()` method using `JwtSecurityTokenHandler`
3. Add proper token validation (expiration, format, signature)
4. Implement proper error logging

```csharp
private bool TokenHasRequiredScope(string token, string requiredScope)
{
    try
    {
        var handler = new JwtSecurityTokenHandler();
        
        if (!handler.CanReadToken(token))
        {
            _logger.LogWarning("Invalid JWT token format");
            return false;
        }
        
        var jwtToken = handler.ReadJwtToken(token);
        
        if (jwtToken.ValidTo < DateTime.UtcNow)
        {
            _logger.LogWarning("JWT token has expired");
            return false;
        }
        
        var scopeClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "scope");
        if (scopeClaim == null)
        {
            _logger.LogWarning("JWT token does not contain scope claim");
            return false;
        }
        
        var scopes = scopeClaim.Value.Split(' ');
        return scopes.Contains(requiredScope);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error validating JWT token for required scope");
        return false;
    }
}
```

## Acceptance Criteria

- [ ] JWT parsing uses `System.IdentityModel.Tokens.Jwt` library
- [ ] Token expiration is checked
- [ ] Scope claims are properly validated
- [ ] Proper error logging with context
- [ ] Unit tests for token validation scenarios
- [ ] Security scan passes

## Files to Modify

- `src/amlink-submissions-mcp-server/Tools/SubmissionApiTools.cs` (lines 204-236)
- `src/amlink-submissions-mcp-server/amlink-submissions-mcp-server.csproj` (add package reference)
- `src/amlink-submissions-mcp.Tests/Tools/SubmissionApiToolsTests.cs` (add JWT validation tests)

## Dependencies

Structured logging task should be completed first for proper error logging.

## Related Documentation

- [TASKS.md - Task 4](../TASKS.md#task-4-improve-jwt-token-handling)
- [POTENTIAL-IMPROVEMENTS.md](docs/POTENTIAL-IMPROVEMENTS.md#4-basic-jwt-token-parsing)
- [JWT Best Practices](https://datatracker.ietf.org/doc/html/rfc8725)

**Priority:** Medium  
**Estimated Effort:** 2 days
EOF

create_issue "Improve JWT Token Handling" "$TEMP_DIR/issue-05.md" "enhancement,security,medium-priority"

# Issue 6: Add HTTP Client Resilience
cat > "$TEMP_DIR/issue-06.md" << 'EOF'
## Description

External API calls have no retry logic, circuit breaker pattern, or timeout configuration beyond HttpClient defaults. This can lead to cascading failures and poor user experience during transient network issues.

## Current Issues

- No retry logic for transient failures
- No circuit breaker pattern
- Single point of failure for downstream services
- Poor user experience during network issues

## Proposed Solution

1. Add NuGet package: `Microsoft.Extensions.Http.Resilience`
2. Configure resilient HTTP client with retry, circuit breaker, and timeouts
3. Update `SubmissionApiTools` to use `IHttpClientFactory`

```csharp
builder.Services.AddHttpClient("SubmissionApi", client =>
{
    client.BaseAddress = new Uri(externalApisConfig!.SubmissionApi.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(
        externalApisConfig.SubmissionApi.UserAgent, 
        externalApisConfig.SubmissionApi.Version));
})
.AddStandardResilienceHandler(options =>
{
    // Configure retry policy
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
    options.Retry.UseJitter = true;
    
    // Configure circuit breaker
    options.CircuitBreaker.FailureRatio = 0.5;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
    
    // Configure timeout
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
});
```

## Acceptance Criteria

- [ ] HTTP clients configured with retry policy
- [ ] Circuit breaker implemented
- [ ] Timeouts properly configured
- [ ] Resilience policies logged and monitored
- [ ] Integration tests for resilience scenarios
- [ ] Documentation updated with resilience patterns

## Files to Modify

- `src/amlink-submissions-mcp-server/Program.cs` (add HTTP client configuration)
- `src/amlink-submissions-mcp-server/Tools/SubmissionApiTools.cs` (use IHttpClientFactory)
- `src/amlink-submissions-mcp-server/amlink-submissions-mcp-server.csproj` (add package reference)

## Related Documentation

- [TASKS.md - Task 5](../TASKS.md#task-5-add-http-client-resilience)
- [POTENTIAL-IMPROVEMENTS.md](docs/POTENTIAL-IMPROVEMENTS.md#5-missing-http-client-resilience)
- [Polly Documentation](https://github.com/App-vNext/Polly)

**Priority:** Medium  
**Estimated Effort:** 2 days
EOF

create_issue "Add HTTP Client Resilience" "$TEMP_DIR/issue-06.md" "enhancement,reliability,medium-priority"

# Issue 7: Add Correlation IDs and Improve Error Context
cat > "$TEMP_DIR/issue-07.md" << 'EOF'
## Description

Currently, there are no correlation IDs for tracing requests across services, making it difficult to debug production issues. Error messages are also generic and lack context.

## Current Issues

- No correlation IDs for request tracing
- Cannot correlate client and server logs
- Generic error messages
- Difficult to debug production issues

## Proposed Solution

1. Create `CorrelationIdMiddleware` to add correlation IDs to all requests
2. Include correlation IDs in all log messages
3. Improve error messages with relevant context
4. Add correlation IDs to error responses

```csharp
// Create Middleware/CorrelationIdMiddleware.cs
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        
        context.Response.Headers.Add(CorrelationIdHeader, correlationId);
        
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
```

## Acceptance Criteria

- [ ] Correlation ID middleware implemented
- [ ] All logs include correlation IDs
- [ ] Error messages include relevant context
- [ ] Correlation IDs passed between services
- [ ] Error responses include correlation IDs
- [ ] Documentation updated with correlation ID usage

## Files to Create/Modify

- `src/amlink-submissions-mcp-server/Middleware/CorrelationIdMiddleware.cs` (new)
- `src/amlink-submissions-mcp-server/Program.cs` (add middleware)
- `src/amlink-submissions-mcp-server/Tools/SubmissionApiTools.cs` (improve error messages)
- `src/amlink-submissions-mcp-client/Services/IMcpService.cs` (add correlation ID support)

## Dependencies

Structured logging task should be completed first.

## Related Documentation

- [TASKS.md - Task 6](../TASKS.md#task-6-add-correlation-ids-and-improve-error-context)
- [POTENTIAL-IMPROVEMENTS.md](docs/POTENTIAL-IMPROVEMENTS.md#6-limited-error-context-and-correlation)

**Priority:** Medium  
**Estimated Effort:** 1-2 days
EOF

create_issue "Add Correlation IDs and Improve Error Context" "$TEMP_DIR/issue-07.md" "enhancement,observability,medium-priority"

# Issue 8: Implement Health Checks
cat > "$TEMP_DIR/issue-08.md" << 'EOF'
## Description

The README mentions health checks at `/health` endpoint, but no implementation exists. Health checks are essential for load balancers, monitoring, and zero-downtime deployments.

## Current Issues

- No health check implementation
- Load balancers cannot determine service health
- No automated health monitoring
- Difficult to implement zero-downtime deployments

## Proposed Solution

1. Add NuGet package: `Microsoft.Extensions.Diagnostics.HealthChecks`
2. Configure health checks for self, Identity Server, and Submission API
3. Create multiple health endpoints (health, ready, live)

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddUrlCheck(
        $"{idsConfig!.Url}/.well-known/openid-configuration",
        "Identity Server",
        failureStatus: HealthStatus.Degraded)
    .AddUrlCheck(
        externalApisConfig!.SubmissionApi.BaseUrl,
        "Submission API",
        failureStatus: HealthStatus.Degraded);

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration
            })
        });
        await context.Response.WriteAsync(result);
    }
});
```

## Acceptance Criteria

- [ ] Health check endpoints implemented
- [ ] Dependencies checked (Identity Server, Submission API)
- [ ] JSON response with detailed status
- [ ] Readiness and liveness probes available
- [ ] Health checks integrated with Docker Compose
- [ ] Health checks documented
- [ ] CI/CD updated to use health checks

## Files to Modify

- `src/amlink-submissions-mcp-server/Program.cs` (add health checks)
- `src/amlink-submissions-mcp-server/amlink-submissions-mcp-server.csproj` (add package reference)
- `docker-compose.yml` (add health check configuration)
- `README.md` (update health check documentation)

## Related Documentation

- [TASKS.md - Task 7](../TASKS.md#task-7-implement-health-checks)
- [POTENTIAL-IMPROVEMENTS.md](docs/POTENTIAL-IMPROVEMENTS.md#7-missing-health-checks)
- [ASP.NET Core Health Checks](https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks)

**Priority:** Medium  
**Estimated Effort:** 1 day
EOF

create_issue "Implement Health Checks" "$TEMP_DIR/issue-08.md" "enhancement,observability,medium-priority"

# Issue 9: Add EditorConfig and Code Style Enforcement
cat > "$TEMP_DIR/issue-09.md" << 'EOF'
## Description

There's no `.editorconfig` file or Roslyn analyzers configured, which can lead to inconsistent code formatting and style issues across the codebase.

## Proposed Solution

1. Create `.editorconfig` at repository root with C# style rules
2. Add Roslyn analyzers to all `.csproj` files
3. Enable code style enforcement in build
4. Update CI pipeline to validate code style

## Acceptance Criteria

- [ ] `.editorconfig` file created with C# style rules
- [ ] Roslyn analyzers added to all projects
- [ ] Code style enforced in build
- [ ] CI pipeline validates code style
- [ ] Existing code follows the style rules (or violations documented)

## Files to Create/Modify

- `.editorconfig` (new)
- `src/amlink-submissions-mcp-server/amlink-submissions-mcp-server.csproj`
- `src/amlink-submissions-mcp-client/amlink-submissions-mcp-client.csproj`
- `src/amlink-submissions-mcp.Tests/amlink-submissions-mcp.Tests.csproj`
- `.github/workflows/ci-cd.yml` (add style validation)

## Related Documentation

- [TASKS.md - Task 8](../TASKS.md#task-8-add-editorconfig-and-code-style-enforcement)
- [POTENTIAL-IMPROVEMENTS.md](docs/POTENTIAL-IMPROVEMENTS.md#8-missing-editorconfig-and-code-style-enforcement)
- [EditorConfig](https://editorconfig.org/)

**Priority:** Low  
**Estimated Effort:** 0.5 days
EOF

create_issue "Add EditorConfig and Code Style Enforcement" "$TEMP_DIR/issue-09.md" "enhancement,code-quality,low-priority"

# Issue 10: Enhance Configuration Validation at Startup
cat > "$TEMP_DIR/issue-10.md" << 'EOF'
## Description

Configuration validation exists but is basic. Enhanced validation with schema checking and early failure would improve debugging and prevent runtime errors.

## Proposed Solution

1. Create comprehensive `ConfigurationValidator` class
2. Validate all configuration sections with detailed error messages
3. Call validation at application startup (fail fast)
4. Include validation in health checks

## Acceptance Criteria

- [ ] Comprehensive configuration validation implemented
- [ ] Validation runs at application startup
- [ ] Clear error messages for invalid configuration
- [ ] URL format validation
- [ ] Enum validation for grant types
- [ ] Required field validation
- [ ] Unit tests for validation logic

## Files to Create/Modify

- `src/amlink-submissions-mcp-server/Configuration/ConfigurationValidator.cs` (new)
- `src/amlink-submissions-mcp-server/Program.cs` (call validator)
- `src/amlink-submissions-mcp-client/Configuration/ConfigurationValidator.cs` (new)
- `src/amlink-submissions-mcp-client/Program.cs` (call validator)
- `src/amlink-submissions-mcp.Tests/Configuration/ConfigurationValidatorTests.cs` (new)

## Related Documentation

- [TASKS.md - Task 9](../TASKS.md#task-9-enhance-configuration-validation-at-startup)
- [POTENTIAL-IMPROVEMENTS.md](docs/POTENTIAL-IMPROVEMENTS.md#9-configuration-validation-at-startup)

**Priority:** Low  
**Estimated Effort:** 1 day
EOF

create_issue "Enhance Configuration Validation at Startup" "$TEMP_DIR/issue-10.md" "enhancement,configuration,low-priority"

# Issue 11: Define API Versioning Strategy
cat > "$TEMP_DIR/issue-11.md" << 'EOF'
## Description

Currently, there's no versioning strategy for MCP tools, which could break clients when changes are made. A clear versioning strategy is needed for API evolution.

## Proposed Solution

1. Define versioning approach (tool names, metadata, or server versioning)
2. Document versioning strategy
3. Create deprecation policy
4. Implement version support in tools

## Acceptance Criteria

- [ ] Versioning strategy documented
- [ ] Version included in tool definitions
- [ ] Backward compatibility plan defined
- [ ] Deprecation policy documented
- [ ] Version negotiation implemented (if applicable)
- [ ] Client SDKs updated to handle versions

## Files to Create/Modify

- `docs/API-VERSIONING.md` (new)
- `src/amlink-submissions-mcp-server/Tools/SubmissionApiTools.cs` (add versioning)
- `README.md` (add versioning section)

## Related Documentation

- [TASKS.md - Task 10](../TASKS.md#task-10-define-api-versioning-strategy)
- [POTENTIAL-IMPROVEMENTS.md](docs/POTENTIAL-IMPROVEMENTS.md#10-api-versioning-strategy)

**Priority:** Low  
**Estimated Effort:** 1 day
EOF

create_issue "Define API Versioning Strategy" "$TEMP_DIR/issue-11.md" "enhancement,api,documentation,low-priority"

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}✓ All issues created successfully!${NC}"
echo -e "${GREEN}========================================${NC}"
echo -e "\nView all issues at: https://github.com/${REPO}/issues"
