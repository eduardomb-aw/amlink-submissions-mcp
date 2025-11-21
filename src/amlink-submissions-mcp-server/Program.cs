using AmLink.Submission.Mcp.Server.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var IsDevelopment = builder.Environment.IsDevelopment();

var serverConfig = builder.Configuration.GetSection(ServerConfiguration.SectionName).Get<ServerConfiguration>();
var idsConfig = builder.Configuration.GetSection(IdentityServerConfiguration.SectionName).Get<IdentityServerConfiguration>();
var externalApisConfig = builder.Configuration.GetSection(ExternalApisConfiguration.SectionName).Get<ExternalApisConfiguration>();

ValidateConfiguration(serverConfig, idsConfig, externalApisConfig);

// Inject IOptions configurations
builder.Services.Configure<IdentityServerConfiguration>(
    builder.Configuration.GetSection(IdentityServerConfiguration.SectionName));
builder.Services.Configure<ExternalApisConfiguration>(
    builder.Configuration.GetSection(ExternalApisConfiguration.SectionName));

// Configure authentication with proper Identity Server 4 settings
ConfigureAuthentication(builder.Services, serverConfig!, idsConfig!, builder.Environment);

builder.Services.AddAuthorization();

// Configure health checks
const string SelfHealthCheckDescription = "Application is running";
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(SelfHealthCheckDescription))
    .AddUrlGroup(
        new Uri($"{idsConfig!.Url}/.well-known/openid-configuration"),
        name: "identity_server",
        failureStatus: HealthStatus.Degraded,
        timeout: TimeSpan.FromSeconds(5))
    .AddUrlGroup(
        new Uri(externalApisConfig!.SubmissionApi.BaseUrl),
        name: "submission_api",
        failureStatus: HealthStatus.Degraded,
        timeout: TimeSpan.FromSeconds(5));

builder.Services.AddHttpContextAccessor();
builder.Services.AddMcpServer()
    .WithToolsFromAssembly()
    .WithHttpTransport();

// Configure HttpClientFactory for Submission API (secured by Identity Server 4)
builder.Services.AddHttpClient("SubmissionApi", client =>
{
    client.BaseAddress = new Uri(externalApisConfig!.SubmissionApi.BaseUrl);
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(
        externalApisConfig.SubmissionApi.UserAgent,
    externalApisConfig.SubmissionApi.Version));
    // Note: Authorization header will be set per-request in the tool implementation
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Map health check endpoints
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
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready") || check.Name == "self",
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString()
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Name == "self",
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString()
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapMcp().RequireAuthorization();

// Display startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
DisplayStartupInfo(serverConfig!, idsConfig!, externalApisConfig!, logger);

if (IsDevelopment)
{
    app.UseDeveloperExceptionPage();
}

// Let ASP.NET Core use ASPNETCORE_URLS environment variable for binding
// This allows Docker containers to bind to all interfaces (0.0.0.0:7072)
app.Run();

static void ValidateConfiguration(ServerConfiguration? serverConfig, IdentityServerConfiguration? idsConfig, ExternalApisConfiguration? externalApisConfig)
{
    if (serverConfig?.Url is null)
    {
        throw new InvalidOperationException("Server:Url configuration is required.");
    }

    if (idsConfig is null)
    {
        throw new InvalidOperationException("IdentityServer configuration section is required.");
    }

    if (string.IsNullOrEmpty(idsConfig.Url))
    {
        throw new InvalidOperationException("IdentityServer:Url configuration is required.");
    }

    if (string.IsNullOrEmpty(idsConfig.ClientId))
    {
        throw new InvalidOperationException("IdentityServer:ClientId configuration is required.");
    }

    if (string.IsNullOrEmpty(idsConfig.GrantType))
    {
        throw new InvalidOperationException("IdentityServer:GrantType configuration is required.");
    }

    if (string.IsNullOrEmpty(idsConfig.Scopes))
    {
        throw new InvalidOperationException("IdentityServer:Scopes configuration is required.");
    }

    if (externalApisConfig is null)
    {
        throw new InvalidOperationException("ExternalApis configuration section is required.");
    }


    if (string.IsNullOrEmpty(externalApisConfig.SubmissionApi?.BaseUrl))
    {
        throw new InvalidOperationException("ExternalApis:SubmissionApi:BaseUrl configuration is required.");
    }

    if (string.IsNullOrEmpty(externalApisConfig.SubmissionApi?.RequiredScope))
    {
        throw new InvalidOperationException("ExternalApis:SubmissionApi:RequiredScope configuration is required.");
    }
}


static void ConfigureAuthentication(
    IServiceCollection services,
    ServerConfiguration serverConfig,
    IdentityServerConfiguration idsConfig,
    IWebHostEnvironment environment)
{
    // Build Identity Server 4 specific configuration
    var authorityUrl = idsConfig.Url.TrimEnd('/');

    // For Identity Server 4, the issuer should match exactly what's in the token
    // This is typically the base URL of the Identity Server
    var validIssuers = new List<string> { authorityUrl };

    // For Identity Server 4, valid audiences depend on your API resource configuration
    // Common patterns:
    // 1. API resource name (as defined in Identity Server)
    // 2. Client ID (for client credentials flow)
    // 3. Custom audience claim value
    var validAudiences = new List<string>();

    // Add the client ID as a valid audience (common for client credentials flow)
    validAudiences.Add(idsConfig.ClientId);

    // Add API resource audiences based on scopes
    // In Identity Server 4, scopes are often tied to API resources
    foreach (var scope in idsConfig.ScopesList)
    {
        // Add the scope itself as a potential audience
        validAudiences.Add(scope);

        // Also add with 'api://' prefix if it's an API scope pattern
        if (!scope.StartsWith("api://"))
        {
            validAudiences.Add($"api://{scope}");
        }
    }

    // Add the server URL as a potential audience (for resource-based validation)
    var serverUri = new Uri(serverConfig.Url);
    validAudiences.Add(serverUri.GetLeftPart(UriPartial.Authority));

    services.AddAuthentication(options =>
    {
        options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // Identity Server 4 authority (must match issuer in tokens)
        options.Authority = authorityUrl;

        // Disable HTTPS requirement in development (enable in production)
        options.RequireHttpsMetadata = !environment.IsDevelopment();

        // Configure token validation parameters for Identity Server 4
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            // Identity Server 4 specific settings
            ValidIssuers = validIssuers,
            ValidAudiences = validAudiences,

            // Clock skew tolerance (Identity Server 4 default is 5 minutes)
            ClockSkew = TimeSpan.FromMinutes(5),

            // Claim mappings for Identity Server 4
            NameClaimType = "name", // or "sub" depending on your setup
            RoleClaimType = "role"
        };

        // Identity Server 4 specific JWT Bearer events
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

                // Extract common Identity Server 4 claims
                var subject = context.Principal?.FindFirstValue("sub");
                var clientId = context.Principal?.FindFirstValue("client_id");
                var name = context.Principal?.FindFirstValue("name") ??
                    context.Principal?.FindFirstValue("preferred_username") ??
                    subject ?? "unknown";
                var scopes = context.Principal?.FindFirstValue("scope");
                var audience = context.Principal?.FindFirstValue("aud");

                logger.LogInformation(
                    "Token validated - Subject: {Subject}, Client: {ClientId}, Name: {Name}, Scopes: {Scopes}, Audience: {Audience}",
                    subject, clientId, name, scopes, audience);

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
             {
                 var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                 logger.LogWarning("Identity Server authentication failed: {Message}", context.Exception.Message);

                 // Log additional details for debugging
                 if (context.Exception is SecurityTokenValidationException secEx)
                 {
                     logger.LogWarning("Token validation details: {Details}", secEx.ToString());
                 }

                 return Task.CompletedTask;
             },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Challenging client to authenticate with Identity Server");
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

                // Log the token for debugging (be careful in production)
                if (context.Token != null && environment.IsDevelopment())
                {
                    logger.LogDebug("Received token: {Token}", context.Token.Substring(0, Math.Min(50, context.Token.Length)) + "...");
                }

                return Task.CompletedTask;
            }
        };
    })
    .AddMcp(options =>
    {
        options.ResourceMetadata = new()
        {
            Resource = new Uri(serverConfig.Url),
            ResourceDocumentation = serverConfig.ResourceDocumentationUrl != null ? new Uri(serverConfig.ResourceDocumentationUrl) : null,
            AuthorizationServers = { new Uri(idsConfig.Url) },
            ScopesSupported = idsConfig.ScopesList.ToList(),
        };
    });
}

static void DisplayStartupInfo(ServerConfiguration serverConfig, IdentityServerConfiguration idsConfig, ExternalApisConfiguration externalApisConfig, ILogger<Program> logger)
{
    logger.LogInformation("Starting MCP server with Identity Server 4 authorization at {ServerUrl}", serverConfig.Url);
    logger.LogInformation("Using Identity Server 4: {IdentityServerUrl}", idsConfig.Url);
    logger.LogInformation("Client ID: {ClientId}", idsConfig.ClientId);
    logger.LogInformation("Grant Type: {GrantType}", idsConfig.GrantType);
    logger.LogInformation("Supported Scopes: {Scopes}", string.Join(", ", idsConfig.ScopesList));
    logger.LogInformation("Submission API: {SubmissionApiUrl} (Scope: {RequiredScope})", externalApisConfig.SubmissionApi.BaseUrl, externalApisConfig.SubmissionApi.RequiredScope);
    logger.LogInformation("Protected Resource Metadata URL: {MetadataUrl}", $"{serverConfig.Url}.well-known/oauth-protected-resource");
    logger.LogInformation("Press Ctrl+C to stop the server");
}