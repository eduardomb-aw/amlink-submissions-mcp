using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace amlink_submissions_mcp.Tests.Bff.TestUtilities;

/// <summary>
/// Test authentication scheme options for unit testing.
/// </summary>
public class TestAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Gets or sets whether the user should be authenticated by default.
    /// </summary>
    public bool IsAuthenticated { get; set; } = false;
    
    /// <summary>
    /// Gets or sets the test user identity name.
    /// </summary>
    public string? UserName { get; set; } = "test-user";
    
    /// <summary>
    /// Gets or sets additional claims for the test user.
    /// </summary>
    public List<Claim> Claims { get; set; } = [];
}

/// <summary>
/// Test authentication handler that simulates authentication without external calls.
/// This allows testing authentication behavior in isolation.
/// </summary>
public class TestAuthenticationHandler : AuthenticationHandler<TestAuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(IOptionsMonitor<TestAuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Options.IsAuthenticated)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, Options.UserName ?? "test-user"),
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim("sub", "test-user-id"),
            new Claim("email", "test@example.com")
        };
        
        claims.AddRange(Options.Claims);

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // For unauthenticated requests, return 401 Unauthorized
        Response.StatusCode = 401;
        return Task.CompletedTask;
    }
}