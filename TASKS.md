# AmLink Submissions MCP - Implementation Tasks

This document outlines the tasks to implement improvements identified in [temp_potential-improvements.md](docs/temp_potential-improvements.md) from historical analysis.

## Task Organization

Tasks are organized by priority and phase:
- **Phase 1 (High Priority)**: Foundation - Testing, validation, and logging
- **Phase 2 (Medium Priority)**: Reliability - Security, resilience, and observability
- **Phase 3 (Low Priority)**: Quality - Code standards and maintenance

---

## Phase 1: Foundation (High Priority)

### Task 1: Add Comprehensive Test Suite

**Priority:** High  
**Estimated Effort:** 3-5 days  
**Labels:** `enhancement`, `testing`, `high-priority`  
**Related Issue:** #2

#### Description
Currently, the repository has only ~7% test coverage with 1 test file containing 4 placeholder tests for 15 production files. This task aims to achieve 80%+ code coverage as specified in Issue #2.

#### Implementation Guidelines

1. **Unit Tests** (Priority 1):
   - `SubmissionApiTools` methods:
     - `GetSubmission()`
     - `CreateSubmission()`
     - `ListSubmissions()`
     - `DeclineSubmission()`
   - Service interfaces:
     - `IMcpService.CreateAuthenticatedClientAsync()`
     - `ITokenService` authentication flow
   - Configuration validation logic

2. **Integration Tests** (Priority 2):
   - End-to-end MCP tool invocation
   - OAuth flow with mock Identity Server
   - Submission API integration with mock backend

3. **Test Infrastructure**:
   - Add test coverage reporting (coverlet)
   - Set up test data builders
   - Create mock/fake implementations of external services
   - Add performance/load tests

#### Acceptance Criteria
- [ ] Unit tests for all MCP tools with ≥90% coverage
- [ ] Unit tests for all services with ≥90% coverage
- [ ] Integration tests for critical workflows
- [ ] Test coverage reporting configured and running in CI
- [ ] Overall code coverage ≥80%
- [ ] All tests passing in CI pipeline

#### Files to Create/Modify
- `src/amlink-submissions-mcp.Tests/Tools/SubmissionApiToolsTests.cs` (new)
- `src/amlink-submissions-mcp.Tests/Services/McpServiceTests.cs` (new)
- `src/amlink-submissions-mcp.Tests/Services/TokenServiceTests.cs` (new)
- `src/amlink-submissions-mcp.Tests/Integration/` (new directory)
- `src/amlink-submissions-mcp.Tests/Helpers/` (new directory for test utilities)
- `src/amlink-submissions-mcp.Tests/amlink-submissions-mcp.Tests.csproj` (add coverlet package)
- `.github/workflows/ci-cd.yml` (add coverage reporting)

#### Dependencies
- None

---

### Task 2: Add Input Validation to All MCP Tools

**Priority:** High  
**Estimated Effort:** 1-2 days  
**Labels:** `enhancement`, `security`, `validation`, `high-priority`

#### Description
Currently, MCP tool methods in `SubmissionApiTools.cs` accept parameters without validation. This can lead to NullReferenceException, poor error messages, and potential security risks.

#### Implementation Guidelines

1. **Add validation to all tool methods:**
   - `GetSubmission(string submissionId)`: Validate non-null, non-empty, and optionally format
   - `CreateSubmission(int accountId, string submissionData)`: Validate accountId > 0 and valid JSON
   - `ListSubmissions(int accountId)`: Validate accountId > 0
   - `DeclineSubmission(string submissionId, string reason)`: Validate both parameters

2. **Validation patterns to implement:**
   ```csharp
   if (string.IsNullOrWhiteSpace(submissionId))
       throw new ArgumentException("Submission ID cannot be null or empty", nameof(submissionId));
   
   if (accountId <= 0)
       throw new ArgumentException("Account ID must be a positive integer", nameof(accountId));
   
   // For JSON data:
   try
   {
       JsonDocument.Parse(submissionData);
   }
   catch (JsonException ex)
   {
       throw new ArgumentException("Submission data must be valid JSON", nameof(submissionData), ex);
   }
   ```

3. **Consider creating a validation helper class** for reusable validation logic

#### Acceptance Criteria
- [ ] All MCP tool methods validate their input parameters
- [ ] Proper ArgumentException thrown with descriptive messages
- [ ] Unit tests for validation edge cases
- [ ] Documentation updated with parameter requirements

#### Files to Modify
- `src/amlink-submissions-mcp-server/Tools/SubmissionApiTools.cs` (add validation to 4 methods)
- `src/amlink-submissions-mcp.Tests/Tools/SubmissionApiToolsTests.cs` (add validation tests)

#### Dependencies
- Should be implemented before or alongside Task 1 (testing)

---

### Task 3: Replace Console.WriteLine with Structured Logging

**Priority:** High  
**Estimated Effort:** 1 day  
**Labels:** `enhancement`, `logging`, `high-priority`

#### Description
There are 15 instances of `Console.WriteLine` in production code that should be replaced with structured logging using `ILogger`. This prevents proper log aggregation, filtering, and integration with monitoring tools.

#### Implementation Guidelines

1. **Update `DisplayStartupInfo()` method** in both server and client:
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

2. **Inject ILogger where needed:**
   - Update method signatures to accept `ILogger<T>`
   - Retrieve logger from DI container in Program.cs

3. **Use appropriate log levels:**
   - `LogInformation` for startup and general info
   - `LogWarning` for non-critical issues
   - `LogError` for errors and exceptions

#### Acceptance Criteria
- [ ] No `Console.WriteLine` statements in production code
- [ ] All logging uses `ILogger` with structured logging patterns
- [ ] Appropriate log levels used
- [ ] Logs include semantic properties (structured data)
- [ ] Logs are properly captured in Application Insights or other log sinks

#### Files to Modify
- `src/amlink-submissions-mcp-server/Program.cs` (DisplayStartupInfo method)
- `src/amlink-submissions-mcp-client/Program.cs` (startup messages)

#### Dependencies
- None

---

## Phase 2: Reliability (Medium Priority)

### Task 4: Improve JWT Token Handling

**Priority:** Medium  
**Estimated Effort:** 2 days  
**Labels:** `enhancement`, `security`, `medium-priority`

#### Description
The current `TokenHasRequiredScope()` method uses manual base64 string parsing, which is error-prone and doesn't validate signatures or claims properly. This should be replaced with the standard JWT library.

#### Implementation Guidelines

1. **Add NuGet package:**
   - `System.IdentityModel.Tokens.Jwt`

2. **Rewrite `TokenHasRequiredScope()` method:**
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

3. **Additional improvements:**
   - Consider caching validated tokens
   - Add token signature validation in production
   - Implement proper token refresh logic

#### Acceptance Criteria
- [ ] JWT parsing uses `System.IdentityModel.Tokens.Jwt` library
- [ ] Token expiration is checked
- [ ] Scope claims are properly validated
- [ ] Proper error logging with context
- [ ] Unit tests for token validation scenarios

#### Files to Modify
- `src/amlink-submissions-mcp-server/Tools/SubmissionApiTools.cs` (lines 204-236)
- `src/amlink-submissions-mcp-server/amlink-submissions-mcp-server.csproj` (add package reference)
- `src/amlink-submissions-mcp.Tests/Tools/SubmissionApiToolsTests.cs` (add JWT validation tests)

#### Dependencies
- Task 3 (logging) should be completed first for proper error logging

---

### Task 5: Add HTTP Client Resilience

**Priority:** Medium  
**Estimated Effort:** 2 days  
**Labels:** `enhancement`, `reliability`, `medium-priority`

#### Description
External API calls have no retry logic, circuit breaker pattern, or timeout configuration. This can lead to cascading failures and poor user experience during transient network issues.

#### Implementation Guidelines

1. **Add NuGet package:**
   - `Microsoft.Extensions.Http.Resilience`

2. **Configure resilient HTTP client** in Program.cs:
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

3. **Update SubmissionApiTools** to use IHttpClientFactory

#### Acceptance Criteria
- [ ] HTTP clients configured with retry policy
- [ ] Circuit breaker implemented
- [ ] Timeouts properly configured
- [ ] Resilience policies logged and monitored
- [ ] Integration tests for resilience scenarios

#### Files to Modify
- `src/amlink-submissions-mcp-server/Program.cs` (add HTTP client configuration)
- `src/amlink-submissions-mcp-server/Tools/SubmissionApiTools.cs` (use IHttpClientFactory)
- `src/amlink-submissions-mcp-server/amlink-submissions-mcp-server.csproj` (add package reference)

#### Dependencies
- None

---

### Task 6: Add Correlation IDs and Improve Error Context

**Priority:** Medium  
**Estimated Effort:** 1-2 days  
**Labels:** `enhancement`, `observability`, `medium-priority`

#### Description
Currently, there are no correlation IDs for tracing requests across services, making it difficult to debug production issues. Error messages are also generic and lack context.

#### Implementation Guidelines

1. **Create Correlation ID Middleware:**
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

2. **Improve error messages with context:**
   - Add correlation IDs to all log messages
   - Include relevant context (submission ID, account ID, etc.)
   - Provide actionable error messages

3. **Structured error responses:**
   - Create error response models
   - Include correlation ID in error responses

#### Acceptance Criteria
- [ ] Correlation ID middleware implemented
- [ ] All logs include correlation IDs
- [ ] Error messages include relevant context
- [ ] Correlation IDs passed between services
- [ ] Documentation updated with correlation ID usage

#### Files to Create/Modify
- `src/amlink-submissions-mcp-server/Middleware/CorrelationIdMiddleware.cs` (new)
- `src/amlink-submissions-mcp-server/Program.cs` (add middleware)
- `src/amlink-submissions-mcp-server/Tools/SubmissionApiTools.cs` (improve error messages)
- `src/amlink-submissions-mcp-client/Services/IMcpService.cs` (add correlation ID support)

#### Dependencies
- Task 3 (logging) should be completed first

---

### Task 7: Implement Health Checks

**Priority:** Medium  
**Estimated Effort:** 1 day  
**Labels:** `enhancement`, `observability`, `medium-priority`

#### Description
The README mentions health checks at `/health` endpoint, but no implementation exists. Health checks are essential for load balancers, monitoring, and zero-downtime deployments.

#### Implementation Guidelines

1. **Add NuGet package:**
   - `Microsoft.Extensions.Diagnostics.HealthChecks`

2. **Configure health checks** in Program.cs:
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
   ```

3. **Create multiple health endpoints:**
   - `/health` - Full health check with JSON response
   - `/health/ready` - Readiness probe for K8s
   - `/health/live` - Liveness probe for K8s

#### Acceptance Criteria
- [ ] Health check endpoints implemented
- [ ] Dependencies checked (Identity Server, Submission API)
- [ ] JSON response with detailed status
- [ ] Readiness and liveness probes available
- [ ] Health checks integrated with Docker Compose
- [ ] Health checks documented

#### Files to Modify
- `src/amlink-submissions-mcp-server/Program.cs` (add health checks)
- `src/amlink-submissions-mcp-server/amlink-submissions-mcp-server.csproj` (add package reference)
- `docker-compose.yml` (add health check configuration)
- `README.md` (update health check documentation)

#### Dependencies
- None

---

## Phase 3: Quality (Low Priority)

### Task 8: Add EditorConfig and Code Style Enforcement

**Priority:** Low  
**Estimated Effort:** 0.5 days  
**Labels:** `enhancement`, `code-quality`, `low-priority`

#### Description
There's no `.editorconfig` file or Roslyn analyzers configured, which can lead to inconsistent code formatting and style issues.

#### Implementation Guidelines

1. **Create `.editorconfig`** at repository root:
   ```ini
   # EditorConfig is awesome: https://EditorConfig.org
   
   root = true
   
   [*]
   charset = utf-8
   insert_final_newline = true
   trim_trailing_whitespace = true
   
   [*.cs]
   indent_style = space
   indent_size = 4
   dotnet_sort_system_directives_first = true
   csharp_new_line_before_open_brace = all
   csharp_new_line_before_catch = true
   csharp_new_line_before_else = true
   csharp_new_line_before_finally = true
   
   [*.{csproj,props,targets}]
   indent_size = 2
   
   [*.{json,yml,yaml}]
   indent_size = 2
   ```

2. **Add Roslyn analyzers** to `.csproj` files:
   ```xml
   <PropertyGroup>
     <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
     <EnableNETAnalyzers>true</EnableNETAnalyzers>
     <AnalysisLevel>latest</AnalysisLevel>
     <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
   </PropertyGroup>
   
   <ItemGroup>
     <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="8.0.0">
       <PrivateAssets>all</PrivateAssets>
       <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
     </PackageReference>
   </ItemGroup>
   ```

#### Acceptance Criteria
- [ ] `.editorconfig` file created with C# style rules
- [ ] Roslyn analyzers added to all projects
- [ ] Code style enforced in build
- [ ] CI pipeline validates code style
- [ ] Existing code follows the style rules

#### Files to Create/Modify
- `.editorconfig` (new)
- `src/amlink-submissions-mcp-server/amlink-submissions-mcp-server.csproj`
- `src/amlink-submissions-mcp-client/amlink-submissions-mcp-client.csproj`
- `src/amlink-submissions-mcp.Tests/amlink-submissions-mcp.Tests.csproj`
- `.github/workflows/ci-cd.yml` (add style validation)

#### Dependencies
- None

---

### Task 9: Enhance Configuration Validation at Startup

**Priority:** Low  
**Estimated Effort:** 1 day  
**Labels:** `enhancement`, `configuration`, `low-priority`

#### Description
Configuration validation exists but is basic. Enhanced validation with schema checking and early failure would improve debugging and prevent runtime errors.

#### Implementation Guidelines

1. **Create comprehensive configuration validator:**
   ```csharp
   // Create Configuration/ConfigurationValidator.cs
   public static class ConfigurationValidator
   {
       public static void ValidateAll(
           ServerConfiguration serverConfig,
           IdentityServerConfiguration idsConfig,
           ExternalApisConfiguration externalApisConfig)
       {
           var errors = new List<string>();

           // Server configuration
           if (serverConfig?.Url is null)
               errors.Add("Server:Url is required");
           else if (!Uri.TryCreate(serverConfig.Url, UriKind.Absolute, out _))
               errors.Add("Server:Url must be a valid absolute URL");

           // ... validate all configuration sections

           if (errors.Any())
           {
               var message = "Configuration validation failed:" + Environment.NewLine +
                            string.Join(Environment.NewLine, errors.Select(e => $"  - {e}"));
               throw new InvalidOperationException(message);
           }
       }
   }
   ```

2. **Call validation at startup:**
   - Validate before building the app
   - Fail fast with clear error messages
   - Include validation in health checks

#### Acceptance Criteria
- [ ] Comprehensive configuration validation implemented
- [ ] Validation runs at application startup
- [ ] Clear error messages for invalid configuration
- [ ] URL format validation
- [ ] Enum validation for grant types
- [ ] Required field validation
- [ ] Unit tests for validation logic

#### Files to Create/Modify
- `src/amlink-submissions-mcp-server/Configuration/ConfigurationValidator.cs` (new)
- `src/amlink-submissions-mcp-server/Program.cs` (call validator)
- `src/amlink-submissions-mcp-client/Configuration/ConfigurationValidator.cs` (new)
- `src/amlink-submissions-mcp-client/Program.cs` (call validator)
- `src/amlink-submissions-mcp.Tests/Configuration/ConfigurationValidatorTests.cs` (new)

#### Dependencies
- None

---

### Task 10: Define API Versioning Strategy

**Priority:** Low  
**Estimated Effort:** 1 day  
**Labels:** `enhancement`, `api`, `documentation`, `low-priority`

#### Description
Currently, there's no versioning strategy for MCP tools, which could break clients when changes are made. A clear versioning strategy is needed for API evolution.

#### Implementation Guidelines

1. **Define versioning approach:**
   - Option A: Version in tool names (e.g., `v1_GetSubmission`)
   - Option B: Version in request metadata
   - Option C: Semantic versioning for entire MCP server

2. **Implement chosen strategy:**
   - Add version to tool definitions
   - Support multiple versions simultaneously
   - Document deprecation policy

3. **Create versioning documentation:**
   - Version upgrade guide
   - Breaking change policy
   - Deprecation timeline

#### Acceptance Criteria
- [ ] Versioning strategy documented
- [ ] Version included in tool definitions
- [ ] Backward compatibility plan defined
- [ ] Deprecation policy documented
- [ ] Version negotiation implemented (if applicable)
- [ ] Client SDKs updated to handle versions

#### Files to Create/Modify
- `docs/API-VERSIONING.md` (new)
- `src/amlink-submissions-mcp-server/Tools/SubmissionApiTools.cs` (add versioning)
- `README.md` (add versioning section)

#### Dependencies
- None

---

## Implementation Order

Recommended implementation order for optimal workflow:

1. **Phase 1 (Parallel):**
   - Task 3 (Logging) - Can be done independently
   - Task 2 (Validation) - Can be done with or before Task 1
   - Task 1 (Testing) - Larger task, can proceed after validation is in place

2. **Phase 2 (Sequential):**
   - Task 4 (JWT) - Depends on Task 3 for proper logging
   - Task 6 (Correlation) - Depends on Task 3 for proper logging
   - Task 5 (Resilience) - Can be parallel with Task 6
   - Task 7 (Health Checks) - Can be parallel with Tasks 4-6

3. **Phase 3 (Any Order):**
   - Task 8 (EditorConfig) - Can be done anytime
   - Task 9 (Configuration) - Can be done anytime
   - Task 10 (Versioning) - Planning/documentation task

---

## Task Progress Tracking

| Task | Priority | Status | Assignee | PR |
|------|----------|--------|----------|-----|
| 1. Comprehensive Test Suite | High | 🔴 Not Started | - | - |
| 2. Input Validation | High | 🔴 Not Started | - | - |
| 3. Structured Logging | High | 🔴 Not Started | - | - |
| 4. JWT Token Handling | Medium | 🔴 Not Started | - | - |
| 5. HTTP Client Resilience | Medium | 🔴 Not Started | - | - |
| 6. Correlation IDs | Medium | 🔴 Not Started | - | - |
| 7. Health Checks | Medium | 🔴 Not Started | - | - |
| 8. EditorConfig | Low | 🔴 Not Started | - | - |
| 9. Configuration Validation | Low | 🔴 Not Started | - | - |
| 10. API Versioning | Low | 🔴 Not Started | - | - |

**Status Key:**
- 🔴 Not Started
- 🟡 In Progress
- 🟢 Completed
- 🔵 Blocked

---

## Related Documentation

- [temp_potential-improvements.md](docs/temp_potential-improvements.md) - Detailed analysis of each improvement
- [Issue #1](https://github.com/eduardomb-aw/amlink-submissions-mcp/issues/1) - Setup Production Deployment
- [Issue #2](https://github.com/eduardomb-aw/amlink-submissions-mcp/issues/2) - Add Comprehensive Test Suite
- [Issue #4](https://github.com/eduardomb-aw/amlink-submissions-mcp/issues/4) - Set up Copilot instructions

---

*Document created: 2025-11-20*  
*Based on: PR #3 - Identify Potential Repository Issues*  
*Source: [temp_potential-improvements.md](docs/temp_potential-improvements.md)*
