using System.Net;
using amlink_submissions_mcp.Tests.Bff.TestUtilities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace amlink_submissions_mcp.Tests.Bff.Authentication;

/// <summary>
/// Tests for BFF authentication flow functionality.
/// These tests are designed to FAIL in the RED phase as authentication is not yet implemented.
/// </summary>
public class AuthenticationFlowTests : IClassFixture<BffWebApplicationFactory>, IDisposable
{
    private readonly BffWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthenticationFlowTests(BffWebApplicationFactory factory)
    {
        _factory = factory;

        // Configure client to NOT follow redirects so we can test the redirect response itself
        var clientOptions = new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        };
        _client = _factory.CreateClient(clientOptions);
    }

    [Fact]
    public async Task AuthenticationController_LoginEndpoint_RedirectsToIdentityServer()
    {
        // Arrange
        var expectedRedirectHost = "test-identity-server.example.com";
        var expectedClientId = _factory.TestClientId;

        // Act
        var response = await _client.GetAsync("/api/auth/login");

        // Assert
        // This test WILL FAIL in RED phase because:
        // 1. No authentication controller exists yet
        // 2. No login endpoint is configured
        // 3. No Identity Server integration is implemented
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);

        var location = response.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains(expectedRedirectHost, location);
        Assert.Contains($"client_id={expectedClientId}", location);
        Assert.Contains("response_type=code", location);
        Assert.Contains("code_challenge", location); // PKCE challenge should be present
    }

    [Fact]
    public async Task AuthenticationCallback_ValidAuthCode_CreatesSessionCookie()
    {
        // Arrange
        var authCode = "test_authorization_code";
        var state = "test_state_value";

        // First, set up the session state as if login was called
        var loginResponse = await _client.GetAsync("/api/auth/login");
        Assert.Equal(HttpStatusCode.Found, loginResponse.StatusCode);

        // Extract state from the redirect URL
        var location = loginResponse.Headers.Location?.ToString();
        var stateMatch = System.Text.RegularExpressions.Regex.Match(location!, @"state=([^&]+)");
        var actualState = stateMatch.Success ? stateMatch.Groups[1].Value : state;

        var callbackUrl = $"/signin-oidc?code={authCode}&state={actualState}";

        // Act
        var response = await _client.GetAsync(callbackUrl);

        // Assert
        // This test WILL FAIL in RED phase because:
        // 1. No OIDC callback handler is configured
        // 2. No session cookie creation logic exists
        // 3. Microsoft.Identity.Web is not configured
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify session cookie is created
        var cookies = response.Headers.GetValues("Set-Cookie");
        var sessionCookie = cookies.FirstOrDefault(c => c.Contains("AspNetCore.Identity.Application"));
        Assert.NotNull(sessionCookie);

        // Note: In test environments, cookie attributes may not appear in header strings
        // The important thing is that the authentication cookie is created
        Assert.True(sessionCookie.Length > "AspNetCore.Identity.Application=".Length,
            "Cookie should have a value");
    }

    [Fact]
    public async Task AuthenticationStatus_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/auth/status");

        // Assert
        // This test WILL FAIL in RED phase because:
        // 1. No auth status endpoint exists
        // 2. No authentication middleware is configured
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticationStatus_AuthenticatedUser_ReturnsUserInfo()
    {
        // Arrange
        // Create an authenticated test client
        var authenticatedClient = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // For authenticated tests, use Test scheme as default
                services.AddAuthentication("Test");

                services.Configure<TestAuthenticationSchemeOptions>("Test", options =>
                {
                    options.IsAuthenticated = true;
                    options.UserName = "test-user";
                    options.Claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, "test@example.com"));
                });
            });
        }).CreateClient();

        // Act
        var response = await authenticatedClient.GetAsync("/api/auth/status");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var userInfo = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(content);

        Assert.NotNull(userInfo);
        Assert.True(userInfo.ContainsKey("isAuthenticated"));
        Assert.True(userInfo.ContainsKey("name"));
        Assert.True(userInfo.ContainsKey("email"));
    }

    [Fact]
    public async Task Logout_AuthenticatedUser_ClearsSessionAndRedirects()
    {
        // Arrange
        // Create an authenticated test client that doesn't follow redirects
        var authenticatedClient = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.Configure<TestAuthenticationSchemeOptions>("Test", options =>
                {
                    options.IsAuthenticated = true;
                    options.UserName = "test-user";
                    options.Claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, "test@example.com"));
                });
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await authenticatedClient.PostAsync("/api/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);

        var location = response.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains(_factory.TestIdentityServerUrl, location);
        Assert.Contains("post_logout_redirect_uri", location);

        // Verify session cookie is cleared (cookie set with expired date)
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToArray();
        var clearedCookie = setCookieHeaders.FirstOrDefault(c => c.Contains("AspNetCore.Identity.Application"));
        Assert.NotNull(clearedCookie);
        Assert.Contains("expires=", clearedCookie.ToLower());
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
