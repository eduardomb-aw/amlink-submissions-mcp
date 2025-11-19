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

// Display startup information
Console.WriteLine("Protected MCP Client");
Console.WriteLine($"Connecting to MCP server at {mcpConfig.Url}...");
Console.WriteLine($"Using Identity Server: {idsConfig.Url}");
Console.WriteLine($"Client ID: {idsConfig.ClientId}");
Console.WriteLine($"Grant Type: {idsConfig.GrantType}");
Console.WriteLine($"Will test Submission API integration secured by Identity Server 4");
Console.WriteLine("Press Ctrl+C to stop the server");

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpClient(); // Add HTTP client factory

// Configure named HTTP client for MCP operations
builder.Services.AddHttpClient("mcp-client", client =>
{
    client.Timeout = TimeSpan.FromMinutes(5); // Adjust timeout as needed
});

// Configure data protection for local development
if (builder.Environment.IsDevelopment())
{
    // For local development, use simple data protection that persists keys
    var keysPath = Path.Combine(builder.Environment.ContentRootPath, "temp-keys");
    Directory.CreateDirectory(keysPath);
    
    builder.Services.AddDataProtection()
        .SetApplicationName("amlink-mcp-client")
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
        .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
}
else
{
    // Configure data protection for containers - use simpler approach
    builder.Services.AddDataProtection()
        .SetApplicationName("amlink-mcp-client");
}

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // For local development, don't require HTTPS
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
        ? CookieSecurePolicy.SameAsRequest 
        : CookieSecurePolicy.Always;
});

// Configure antiforgery for local development
builder.Services.AddAntiforgery(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    }
});

// Register services
builder.Services.AddSingleton<IOAuthStateService, OAuthStateService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IMcpService, McpService>();
builder.Services.AddSingleton(mcpConfig!);
builder.Services.AddSingleton(idsConfig!);

var app = builder.Build();

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

// Only redirect to HTTPS in production or when HTTPS is available
if (!app.Environment.IsDevelopment() || 
    app.Configuration.GetValue<string>("ASPNETCORE_URLS")?.Contains("https") == true)
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

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