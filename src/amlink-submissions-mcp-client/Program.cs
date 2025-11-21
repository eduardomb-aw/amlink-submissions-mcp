using AmLink.Submissions.Mcp.Client.Configuration;
using AmLink.Submissions.Mcp.Client.Services;
using System.Collections.Concurrent;
using Microsoft.Extensions.AI;
using OpenAI;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Bind configuration sections to strongly typed objects
var mcpConfig = builder.Configuration.GetSection(McpClientConfiguration.SectionName).Get<McpClientConfiguration>();
var idsConfig = builder.Configuration.GetSection(IdentityServerConfiguration.SectionName).Get<IdentityServerConfiguration>();

// Validate required configuration
if (mcpConfig?.Url is null)
{
    throw new InvalidOperationException("McpServer:Url configuration is missing.");
}

if (idsConfig is null)
{
    throw new InvalidOperationException("IdentityServer configuration section is missing.");
}

if (string.IsNullOrEmpty(idsConfig.ClientId))
{
    throw new InvalidOperationException("IdentityServer:ClientId configuration is missing.");
}

if (string.IsNullOrEmpty(idsConfig.ClientSecret))
{
    throw new InvalidOperationException("IdentityServer:ClientSecret configuration is required. Consider using user secrets or environment variables for production.");
}

var openAiKey = builder.Configuration.GetValue<string>("OPENAI_API_KEY");
var openAIClient = new OpenAIClient(openAiKey).GetChatClient("gpt-4o-mini");

// Create a sampling client.
using IChatClient samplingClient = openAIClient.AsIChatClient()
    .AsBuilder()
    .Build();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpClient(); // Add HTTP client factory

// Configure named HTTP client for MCP operations
builder.Services.AddHttpClient("mcp-client", client =>
{
    client.Timeout = TimeSpan.FromMinutes(5); // Adjust timeout as needed
});

// Configure HTTP client for OpenAI
builder.Services.AddHttpClient("openai", client =>
{
    client.Timeout = TimeSpan.FromMinutes(2);
});

// Configure data protection with consistent behavior
// Use the Docker volume path that matches docker-compose.yml
var keyRingPath = builder.Configuration.GetValue<string>("DataProtection:KeyRingPath") ?? "/root/.aspnet/DataProtection-Keys";

// Ensure the key ring directory exists
Directory.CreateDirectory(keyRingPath);

builder.Services.AddDataProtection()
    .SetApplicationName("amlink-mcp-client")
    .PersistKeysToFileSystem(new DirectoryInfo(keyRingPath))
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90)); // Longer lifetime for stability

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "AmLinkMcpSession"; // Unique session cookie name
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Consistent behavior
    options.Cookie.SameSite = SameSiteMode.Lax; // More permissive for OAuth flows
});

// Configure antiforgery with consistent behavior
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = "__RequestVerificationToken";
    options.Cookie.Expiration = TimeSpan.FromMinutes(20); // Shorter expiration to reduce stale token issues
    options.Cookie.IsEssential = true;
    options.SuppressXFrameOptionsHeader = false;
});

// Register services
builder.Services.AddSingleton<IOAuthStateService, OAuthStateService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IMcpService, McpService>();
builder.Services.AddScoped<IOpenAiService, OpenAiService>();
builder.Services.AddSingleton(mcpConfig!);
builder.Services.AddSingleton(idsConfig!);

var app = builder.Build();

// Display startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Protected MCP Client");
logger.LogInformation("Connecting to MCP server at {McpServerUrl}", mcpConfig.Url);
logger.LogInformation("Using Identity Server: {IdentityServerUrl}", idsConfig.Url);
logger.LogInformation("Client ID: {ClientId}", idsConfig.ClientId);
logger.LogInformation("Grant Type: {GrantType}", idsConfig.GrantType);
logger.LogInformation("Will test Submission API integration secured by Identity Server 4");
logger.LogInformation("Press Ctrl+C to stop the client");
logger.LogInformation("Data protection keys will be stored in: {KeyRingPath}", keyRingPath);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    // In development, show detailed errors
    app.UseDeveloperExceptionPage();
}

// Configure HTTPS redirection based on environment
// In Azure Web Apps, HTTPS termination happens at the load balancer
var useHttpsRedirection = app.Environment.IsDevelopment() && 
                         app.Configuration.GetValue<string>("ASPNETCORE_URLS")?.Contains("https") == true;

if (useHttpsRedirection)
{
    app.UseHttpsRedirection();
}

// Configure static files with proper caching and content types
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Set cache headers for static files
        const int durationInSeconds = 60 * 60 * 24 * 30; // 30 days
        ctx.Context.Response.Headers.Append("Cache-Control", $"public,max-age={durationInSeconds}");
        
        // Ensure proper content types for common files
        var extension = Path.GetExtension(ctx.File.Name).ToLowerInvariant();
        switch (extension)
        {
            case ".css":
                ctx.Context.Response.ContentType = "text/css";
                break;
            case ".js":
                ctx.Context.Response.ContentType = "application/javascript";
                break;
            case ".woff":
            case ".woff2":
                ctx.Context.Response.ContentType = "font/woff";
                break;
        }
    }
});

app.UseRouting();
app.UseSession();

app.UseAuthorization();

app.MapRazorPages();

// Add OAuth callback endpoint
app.MapGet("/oauth/callback", async (HttpContext context, ITokenService tokenService) =>
{
    var query = context.Request.Query;
    var code = query["code"].FirstOrDefault();
    var state = query["state"].FirstOrDefault();
    var error = query["error"].FirstOrDefault();
    
    if (!string.IsNullOrEmpty(error))
    {
        var errorDescription = query["error_description"].FirstOrDefault();
        return Results.Content($@"
            <html>
            <head><title>Authentication Error</title></head>
            <body>
                <h1>Authentication Error</h1>
                <p>Error: {error}</p>
                <p>Description: {errorDescription}</p>
                <p><a href='/' onclick='window.opener && window.opener.location.reload(); window.close();'>Return to App</a></p>
            </body>
            </html>
        ", "text/html");
    }
    
    if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
    {
        return Results.Content(@"
            <html>
            <head><title>Authentication Error</title></head>
            <body>
                <h1>Authentication Error</h1>
                <p>Missing authorization code or state parameter</p>
                <p><a href='/' onclick='window.opener && window.opener.location.reload(); window.close();'>Return to App</a></p>
            </body>
            </html>
        ", "text/html");
    }
    
    var success = await tokenService.CompleteAuthenticationAsync(code, state);
    
    if (success)
    {
        return Results.Content(@"
            <html>
            <head><title>Authentication Complete</title></head>
            <body>
                <h1>Authentication Complete!</h1>
                <p>You can close this window now. The application will continue automatically.</p>
                <script>
                    // Notify parent window and close popup
                    if (window.opener) {
                        window.opener.postMessage('auth-success', '*');
                        window.close();
                    } else {
                        // Fallback for non-popup scenario
                        setTimeout(() => window.location.href = '/', 2000);
                    }
                </script>
            </body>
            </html>
        ", "text/html");
    }
    else
    {
        return Results.Content(@"
            <html>
            <head><title>Authentication Failed</title></head>
            <body>
                <h1>Authentication Failed</h1>
                <p>Failed to complete authentication. Please try again.</p>
                <p><a href='/' onclick='window.opener && window.opener.location.reload(); window.close();'>Return to App</a></p>
            </body>
            </html>
        ", "text/html");
    }
});

app.Run();

// Keep existing interfaces and classes for backward compatibility
public interface IOAuthStateService
{
    string CreateAuthorizationRequest(Uri authorizationUrl);
    Task<string?> WaitForAuthorizationCodeAsync(string state, CancellationToken cancellationToken);
    void SetAuthResult(string? state, string? code, string? error);
}

public class OAuthStateService : IOAuthStateService
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<AuthResult>> _pendingRequests = new();
    private readonly ConcurrentDictionary<string, Uri> _authUrls = new();

    public string CreateAuthorizationRequest(Uri authorizationUrl)
    {
        var state = Guid.NewGuid().ToString();
        _authUrls[state] = authorizationUrl;
        _pendingRequests[state] = new TaskCompletionSource<AuthResult>();
        return state;
    }

    public async Task<string?> WaitForAuthorizationCodeAsync(string state, CancellationToken cancellationToken)
    {
        if (!_pendingRequests.TryGetValue(state, out var tcs))
            return null;

        try
        {
            using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());
            var result = await tcs.Task;
            return result.Code;
        }
        finally
        {
            _pendingRequests.TryRemove(state, out _);
            _authUrls.TryRemove(state, out _);
        }
    }

    public void SetAuthResult(string? state, string? code, string? error)
    {
        if (state != null && _pendingRequests.TryGetValue(state, out var tcs))
        {
            if (!string.IsNullOrEmpty(error))
            {
                tcs.TrySetException(new InvalidOperationException(error));
            }
            else
            {
                tcs.TrySetResult(new AuthResult { Code = code, Error = error });
            }
        }
    }

    private class AuthResult
    {
        public string? Code { get; set; }
        public string? Error { get; set; }
    }
}

public class OAuthRedirectRequiredException : Exception
{
    public string AuthorizationUrl { get; }
    
    public OAuthRedirectRequiredException(string authorizationUrl) 
        : base($"OAuth authentication required. Redirect user to: {authorizationUrl}")
    {
        AuthorizationUrl = authorizationUrl;
    }
}