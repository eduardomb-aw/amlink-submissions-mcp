using System.IO;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace amlink_submissions_mcp.Tests.Bff.TestUtilities;

/// <summary>
/// Custom web application factory for testing BFF functionality.
/// Creates a minimal setup for GREEN phase testing without external dependencies.
/// </summary>
public class BffWebApplicationFactory : WebApplicationFactory<amlink_submissions_mcp_bff.BffProgramMarker>
{
    private const string TestIdentityServerUrlValue = "https://test-identity-server.example.com";
    private const string TestClientIdValue = "test-bff-client";
    
    protected override IHostBuilder CreateHostBuilder()
    {
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseEnvironment("Test");
                
                // Configure content root to avoid directory not found errors
                var contentRoot = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                webBuilder.UseContentRoot(contentRoot!);
                
                webBuilder.ConfigureServices(services =>
                {
                    // Start fresh with only the services we need for testing
                    services.AddDistributedMemoryCache();
                    services.AddSession(options =>
                    {
                        options.Cookie.Name = "TestBFF.Session";
                        options.Cookie.HttpOnly = true;
                        options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Allow HTTP in tests
                        options.Cookie.SameSite = SameSiteMode.Strict;
                        options.IdleTimeout = TimeSpan.FromHours(1);
                    });
                    
                    // Add controllers from the BFF project
                    services.AddControllers()
                        .AddApplicationPart(typeof(amlink_submissions_mcp_bff.BffProgramMarker).Assembly);
                    
                    services.AddRouting();
                    
                    // Add authentication with both test handler and cookies for sign-in
                    services.AddAuthentication(defaultScheme: "Cookies")
                        .AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options => { })
                        .AddCookie("Cookies", options =>
                        {
                            options.Cookie.Name = "AspNetCore.Identity.Application";
                            options.Cookie.HttpOnly = true;
                            options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Force Secure flag for testing
                            options.Cookie.SameSite = SameSiteMode.Strict;
                            options.LoginPath = "/api/auth/login";
                            options.AccessDeniedPath = "/api/auth/login";
                            
                            // Configure API challenge behavior
                            options.Events.OnRedirectToLogin = context =>
                            {
                                // For API requests, return 401 with WWW-Authenticate header instead of redirect
                                if (context.Request.Path.StartsWithSegments("/api"))
                                {
                                    context.Response.StatusCode = 401;
                                    context.Response.Headers.Append("WWW-Authenticate", "Bearer realm=\"BFF\"");
                                    return Task.CompletedTask;
                                }
                                
                                // For non-API requests, use default redirect behavior
                                context.Response.Redirect(context.RedirectUri);
                                return Task.CompletedTask;
                            };
                        });
                    
                    services.AddAuthorization();
                    
                    // Add HTTP context accessor for controllers that need it
                    services.AddHttpContextAccessor();
                });
                
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseSession();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                        endpoints.MapGet("/health", () => "Healthy");
                    });
                });
            });
        
        return hostBuilder;
    }

    /// <summary>
    /// Gets the configured test Identity Server URL.
    /// </summary>
    public string TestIdentityServerUrl => TestIdentityServerUrlValue;
    
    /// <summary>
    /// Gets the configured test client ID.
    /// </summary>
    public string TestClientId => TestClientIdValue;
}

